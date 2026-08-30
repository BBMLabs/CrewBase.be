using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Campaigns;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class BoatRepository(TenantDbContext context) : IBoatRepository
{
    public Task<List<Boat>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Boats.ToListAsync(cancellationToken);

    public Task<Boat?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Boats.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<List<Boat>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken) =>
        context.Boats.Where(b => b.BranchId == branchId).ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        context.Boats.CountAsync(b => b.IsActive, cancellationToken);

    public void Add(Boat boat) => context.Boats.Add(boat);

    public void Remove(Boat boat) => context.Boats.Remove(boat);
}

public sealed class BranchRepository(TenantDbContext context) : IBranchRepository
{
    public Task<List<Branch>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Branches.ToListAsync(cancellationToken);

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Branches.FirstOrDefaultAsync(b => b.Code == code, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Branches.AnyAsync(b => b.Code == code, cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        context.Branches.CountAsync(b => b.IsActive, cancellationToken);

    public void Add(Branch branch) => context.Branches.Add(branch);

    public void Remove(Branch branch) => context.Branches.Remove(branch);
}

public sealed class InstructorRepository(TenantDbContext context) : IInstructorRepository
{
    public Task<List<Instructor>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Instructors.ToListAsync(cancellationToken);

    public Task<Instructor?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Instructors.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<List<Instructor>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken) =>
        context.Instructors.Where(i => i.BranchId == branchId).ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        context.Instructors.CountAsync(i => i.IsActive, cancellationToken);

    public void Add(Instructor instructor) => context.Instructors.Add(instructor);

    public void Remove(Instructor instructor) => context.Instructors.Remove(instructor);
}

public sealed class LessonPackageRepository(TenantDbContext context) : ILessonPackageRepository
{
    public Task<List<LessonPackage>> GetAllAsync(CancellationToken cancellationToken) =>
        context.LessonPackages.ToListAsync(cancellationToken);

    public Task<LessonPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.LessonPackages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(LessonPackage lessonPackage) => context.LessonPackages.Add(lessonPackage);

    public void Remove(LessonPackage lessonPackage) => context.LessonPackages.Remove(lessonPackage);
}

public sealed class CampaignRepository(TenantDbContext context) : ICampaignRepository
{
    public Task<List<Campaign>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Campaigns.ToListAsync(cancellationToken);

    public Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Campaign campaign) => context.Campaigns.Add(campaign);

    public void Remove(Campaign campaign) => context.Campaigns.Remove(campaign);
}

public sealed class SettingsRepository(TenantDbContext context) : ISettingsRepository
{
    public Task<CompanySettings?> GetAsync(CancellationToken cancellationToken) =>
        context.Settings.Include(s => s.DaySchedules).FirstOrDefaultAsync(cancellationToken);

    public void Add(CompanySettings settings) => context.Settings.Add(settings);
}

public sealed class TrainingSessionRepository(TenantDbContext context) : ITrainingSessionRepository
{
    public async Task<TrainingSession?> FindJoinableAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, int level, CancellationToken cancellationToken)
    {
        // Kapasite (iptaller hariç aktif üye sayısı) hesabı domain'de; adaylar çekilip bellekte süzülür.
        var candidates = await context.TrainingSessions
            .Include(s => s.Appointments)
            .Include(s => s.Boat)
            .Include(s => s.Instructor)
            .Where(s => s.Date == date && s.StartTime == startTime &&
                        s.BoatClass == boatClass && s.Level == level)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(s => s.HasFreeSeat);
    }

    public Task<List<TrainingSession>> GetBySlotAsync(
        DateOnly date, TimeOnly startTime, CancellationToken cancellationToken) =>
        context.TrainingSessions
            .Where(s => s.Date == date && s.StartTime == startTime)
            .ToListAsync(cancellationToken);

    public Task<List<TrainingSession>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken) =>
        context.TrainingSessions
            .Include(s => s.Appointments)
            .ThenInclude(a => a.Customer)
            .Include(s => s.Boat)
            .Include(s => s.Instructor)
            .Where(s => s.Date == date)
            .ToListAsync(cancellationToken);

    public Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.TrainingSessions
            .Include(s => s.Appointments)
            .ThenInclude(a => a.Customer)
            .Include(s => s.Boat)
            .Include(s => s.Instructor)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(TrainingSession session) => context.TrainingSessions.Add(session);
}
