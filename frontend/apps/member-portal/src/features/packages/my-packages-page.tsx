import { useQuery } from "@tanstack/react-query";
import { AsyncBoundary, Badge, EmptyState, PageHeader } from "@crewbase/design-system";
import { useApi } from "../../app/api-context";

export function MyPackagesPage() {
  const api = useApi();
  const packages = useQuery({ queryKey: ["member", "packages"], queryFn: () => api.member.packages() });

  return (
    <div className="flex flex-col gap-5">
      <PageHeader title="Paketlerim" description="Ders paketleriniz ve kalan dersleriniz." />
      <AsyncBoundary state={packages} isEmpty={(packages.data?.length ?? 0) === 0}>
        {(packages.data?.length ?? 0) === 0 ? (
          <EmptyState title="Paketiniz yok" description="Kulübünüzden ders paketi satın alabilirsiniz; tanımlandığında burada görünür." />
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {(packages.data ?? []).map((pkg) => {
              const exhausted = pkg.remainingSessions === 0;
              return (
                <li key={pkg.id} className="flex flex-col gap-2 rounded-lg border border-line bg-surface p-4 shadow-xs">
                  <div className="flex items-center justify-between gap-2">
                    <p className="font-medium text-ink">{pkg.packageName}</p>
                    {exhausted ? <Badge tone="danger">Bitti</Badge> : <Badge tone="success">Aktif</Badge>}
                  </div>
                  <p className="font-display text-2xl font-semibold text-brand-700">{pkg.remainingSessions}</p>
                  <p className="text-xs text-ink-3">kalan / {pkg.totalSessions} ders</p>
                </li>
              );
            })}
          </ul>
        )}
      </AsyncBoundary>
    </div>
  );
}
