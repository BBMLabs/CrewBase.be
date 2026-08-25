using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.ResetCompanyAdminPassword;

public sealed record ResetCompanyAdminPasswordCommand(Guid CompanyId) : ICommand<Unit>;
