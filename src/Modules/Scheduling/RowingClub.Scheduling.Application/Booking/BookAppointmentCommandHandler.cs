using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Consents;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Booking;

public sealed class BookAppointmentCommandHandler(
    ISettingsRepository settingsRepository,
    IClosedDateRepository closedDateRepository,
    IConsentRecordRepository consentRepository,
    ICustomerRepository customerRepository,
    IAppointmentRepository appointmentRepository,
    ITrainingSessionRepository sessionRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<BookAppointmentCommand, BookAppointmentResponse>
{
    public async Task<BookAppointmentResponse> Handle(
        BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        settings.EnsureBookable(request.Date, request.StartTime);

        if (await closedDateRepository.IsClosedAsync(request.Date, cancellationToken))
            throw new DomainException("closed_date", "Firma bu tarihte randevuya kapalıdır.");

        var boatClass = BoatClassExtensions.Parse(request.BoatClass);

        // Kimlik eşleştirme: önce telefon, bulunamazsa e-posta ile mevcut üye/misafir kaydı aranır.
        // Böylece giriş yapmadan rezervasyon deneyen bir üye, kayıtlı DERECESİYLE gruplanır.
        var customer = await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken);
        if (customer is null && !string.IsNullOrWhiteSpace(request.Email))
            customer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (customer is null)
        {
            customer = Customer.Create(request.FullName, request.Phone, request.Email);
            customerRepository.Add(customer);
        }
        else
        {
            customer.UpdateContact(request.FullName, request.Email);

            if (await appointmentRepository.HasActiveForCustomerAtAsync(
                    customer.Id, request.Date, request.StartTime, cancellationToken))
            {
                throw new DomainException("already_booked", "Bu saat için zaten bir randevunuz var.");
            }
        }

        // Üyelik hesabı olmayanlar 1x/2x için deneyim onayı vermelidir; onay yoksa reddedilir.
        if (!customer.HasAccount && boatClass != BoatClass.Quad4x && !request.ExperienceAcknowledged)
        {
            throw new DomainException(
                "guest_class_restricted",
                "1x ve 2x tekneler deneyimli kürekçiler içindir. Devam etmek için deneyim onayını işaretleyin.");
        }

        // Randevu beyanları (yüzme, sağlık, kurallar, KVKK): kayıtlı üye BİR KEZ onaylar
        // (önceki kabulleri geçerli sayılır), misafir HER randevuda yeniden onaylar.
        await ConsentGuard.EnforceAsync(
            consentRepository, customer.Id, ConsentScope.Booking,
            request.AcceptedConsents, request.IpAddress,
            trustExisting: customer.HasAccount, cancellationToken);

        var reminderMinutes = ResolveReminder(settings, customer, request.ReminderMinutes);

        // Paketten düşerek alım: kalan dersi olan en eski paket kullanılır, iptalde iade edilir.
        CustomerPackage? usedPackage = null;
        if (request.UsePackage)
        {
            usedPackage = (await customerPackageRepository.GetByCustomerAsync(customer.Id, cancellationToken))
                .Where(p => p.RemainingSessions > 0)
                .OrderBy(p => p.AssignedAtUtc)
                .FirstOrDefault()
                ?? throw new DomainException("no_active_package", "Kullanılabilir ders paketiniz yok.");

            usedPackage.Deduct();
        }

        TrainingSession? session = null;
        if (boatClass == BoatClass.Double2x && !string.IsNullOrWhiteSpace(request.TeammateName))
        {
            session = await sessionRepository.FindMutualTeammateSessionAsync(
                request.Date, request.StartTime, boatClass, request.FullName, request.TeammateName, cancellationToken);
        }

        if (session is null && !customer.HasAccount && boatClass == BoatClass.Double2x)
        {
            session = await sessionRepository.FindJoinableLowestLevelAsync(
                request.Date, request.StartTime, boatClass, cancellationToken);

            if (session is null)
            {
                throw new DomainException(
                    "no_2x_partner_available",
                    "Bu saatte size eşlik edecek bir 2x ekip bulunamadı. Lütfen başka bir saat deneyin veya 4x seçin.");
            }
        }
        else if (session is null)
        {
            // Aynı slot + sınıf + derecede boş koltuğu olan seans varsa üye ona gruplanır;
            // yoksa boş tekne ve müsait eğitmenle yeni seans açılır.
            session = await sessionRepository.FindJoinableAsync(
                request.Date, request.StartTime, boatClass, customer.Level, cancellationToken);

            session ??= await OpenSessionAsync(request.Date, request.StartTime, boatClass, customer.Level, cancellationToken);
        }

        var appointment = Appointment.Book(customer, session, request.Note, reminderMinutes, usedPackage?.Id, request.TeammateName);
        appointmentRepository.Add(appointment);

        memberLogRepository.Add(MemberLog.Record(
            customer.Id, MemberEvents.AppointmentBooked,
            $"{session.Date:yyyy-MM-dd} {session.StartTime:HH\\:mm} {session.BoatClass.Label()}"));

        if (usedPackage is not null)
        {
            memberLogRepository.Add(MemberLog.Record(
                customer.Id, MemberEvents.PackageDeducted,
                $"{usedPackage.PackageName}: kalan {usedPackage.RemainingSessions} ders"));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BookAppointmentResponse(
            appointment.Id, session.Id, session.Date, session.StartTime,
            session.BoatClass.Label(), session.Level,
            session.Boat?.Name, session.Instructor?.FullName,
            appointment.ReminderMinutes, appointment.Status.ToString());
    }

    private static int? ResolveReminder(CompanySettings settings, Customer customer, int? requested)
    {
        // null = üyenin tercihi, o da yoksa firma varsayılanı; 0 = hatırlatma istemiyorum;
        // diğerleri firmanın sunduğu seçeneklerden biri olmak zorunda.
        if (requested is null)
        {
            var preferred = customer.DefaultReminderMinutes ?? settings.DefaultReminderMinutes;
            return preferred == 0 ? null : preferred;
        }

        if (requested == 0)
            return null;

        if (!settings.ReminderOptions().Contains(requested.Value))
            throw new DomainException("invalid_reminder", "Seçilen hatırlatma süresi firmanın sunduğu seçenekler arasında değil.");

        return requested;
    }

    private async Task<TrainingSession> OpenSessionAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, int level, CancellationToken cancellationToken)
    {
        var slotSessions = await sessionRepository.GetBySlotAsync(date, startTime, cancellationToken);

        var boatsOfClass = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.IsActive && b.Class == boatClass)
            .ToList();

        Boat? boat = null;
        if (boatsOfClass.Count > 0)
        {
            var usedBoatIds = slotSessions.Where(s => s.BoatId is not null).Select(s => s.BoatId!.Value).ToHashSet();
            boat = boatsOfClass.FirstOrDefault(b => !usedBoatIds.Contains(b.Id))
                ?? throw new DomainException(
                    "slot_full", $"Bu saatte {boatClass.Label()} teknelerinin tümü dolu. Lütfen başka bir saat seçin.");
        }
        else if (boatClass != BoatClass.Single1x)
        {
            // Firma bu sınıfta tekne tanımlamadıysa grup seansı açılamaz; bireysel (1x) seans
            // teknesiz de ilerleyebilir.
            throw new DomainException(
                "boat_class_unavailable", $"Bu firma için {boatClass.Label()} teknesi tanımlı değil.");
        }

        var busyInstructorIds = slotSessions
            .Where(s => s.InstructorId is not null)
            .Select(s => s.InstructorId!.Value)
            .ToHashSet();

        var instructor = (await instructorRepository.GetAllAsync(cancellationToken))
            .Where(i => i.IsActive)
            .FirstOrDefault(i => !busyInstructorIds.Contains(i.Id));

        var session = TrainingSession.Create(date, startTime, boatClass, level, boat?.Id, instructor?.Id);
        if (boat is not null)
            session.AssignBoat(boat);
        if (instructor is not null)
            session.AssignInstructor(instructor);

        sessionRepository.Add(session);
        return session;
    }
}
