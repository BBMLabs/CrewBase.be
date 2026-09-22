using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Appointments;
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

    public Task<List<Branch>> GetPageAsync(
        string? search, bool? isActive, string? cursorName, Guid? cursorId, int take, CancellationToken cancellationToken)
    {
        var query = context.Branches.AsQueryable();

        if (isActive is { } active)
            query = query.Where(b => b.IsActive == active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Name, $"%{term}%") ||
                (b.Address != null && EF.Functions.ILike(b.Address, $"%{term}%")) ||
                (b.Phone != null && EF.Functions.ILike(b.Phone, $"%{term}%")) ||
                (b.ManagerName != null && EF.Functions.ILike(b.ManagerName, $"%{term}%")));
        }

        if (cursorName is not null && cursorId is { } id)
            query = query.Where(b => b.Name.CompareTo(cursorName) > 0 || (b.Name == cursorName && b.Id.CompareTo(id) > 0));

        return query.OrderBy(b => b.Name).ThenBy(b => b.Id).Take(take).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, bool? isActive, CancellationToken cancellationToken)
    {
        var query = context.Branches.AsQueryable();

        if (isActive is { } active)
            query = query.Where(b => b.IsActive == active);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Name, $"%{term}%") ||
                (b.Address != null && EF.Functions.ILike(b.Address, $"%{term}%")) ||
                (b.Phone != null && EF.Functions.ILike(b.Phone, $"%{term}%")) ||
                (b.ManagerName != null && EF.Functions.ILike(b.ManagerName, $"%{term}%")));
        }

        return query.CountAsync(cancellationToken);
    }

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

    public Task<List<LessonPackage>> GetPageAsync(
        string? search, decimal? cursorPrice, Guid? cursorId, int take, CancellationToken cancellationToken)
    {
        var query = context.LessonPackages.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%"));
        }

        if (cursorPrice is { } price && cursorId is { } id)
            query = query.Where(p => p.Price.CompareTo(price) > 0 || (p.Price == price && p.Id.CompareTo(id) > 0));

        return query.OrderBy(p => p.Price).ThenBy(p => p.Id).Take(take).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken)
    {
        var query = context.LessonPackages.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%"));
        }

        return query.CountAsync(cancellationToken);
    }

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

    public async Task<TrainingSession?> FindJoinableLowestLevelAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, CancellationToken cancellationToken)
    {
        var candidates = await context.TrainingSessions
            .Include(s => s.Appointments)
            .Include(s => s.Boat)
            .Include(s => s.Instructor)
            .Where(s => s.Date == date && s.StartTime == startTime && s.BoatClass == boatClass)
            .OrderBy(s => s.Level)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(s => s.HasFreeSeat);
    }

    public async Task<TrainingSession?> FindMutualTeammateSessionAsync(
        DateOnly date, TimeOnly startTime, BoatClass boatClass,
        string callerFullName, string teammateFullName, CancellationToken cancellationToken)
    {
        var candidates = await context.TrainingSessions
            .Include(s => s.Appointments).ThenInclude(a => a.Customer)
            .Include(s => s.Boat)
            .Include(s => s.Instructor)
            .Where(s => s.Date == date && s.StartTime == startTime && s.BoatClass == boatClass)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(s =>
            s.HasFreeSeat &&
            s.Appointments.Any(a =>
                a.Status != AppointmentStatus.Cancelled &&
                a.TeammateName != null &&
                string.Equals(a.Customer.FullName.Trim(), teammateFullName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.TeammateName.Trim(), callerFullName.Trim(), StringComparison.OrdinalIgnoreCase)));
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
