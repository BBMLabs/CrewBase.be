import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { PackageDto } from "@crewbase/api-types";
import { z } from "zod";
import {
  AsyncBoundary,
  Badge,
  Button,
  ConfirmDialog,
  CrewAvatar,
  Dialog,
  EmptyState,
  FilterBar,
  FormField,
  Input,
  MeterBar,
  PageHeader,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { formatIsoDateTr } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";

export function PackagesPage() {
  const [tab, setTab] = useState<"catalog" | "balances">("catalog");
  return (
    <div className="flex flex-col gap-5">
      <PageHeader title="Paketler" description="Ders paketi kataloğu ve tüm üyelerin bakiyeleri." />
      <div role="tablist" aria-label="Paket görünümleri" className="flex gap-1">
        <button
          type="button"
          role="tab"
          aria-selected={tab === "catalog"}
          onClick={() => setTab("catalog")}
          className={[
            "rounded-md border px-4 py-2 text-sm font-medium",
            tab === "catalog" ? "border-brand-600 bg-brand-50 text-brand-700" : "border-line bg-surface text-ink-2",
          ].join(" ")}
        >
          Katalog
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === "balances"}
          onClick={() => setTab("balances")}
          className={[
            "rounded-md border px-4 py-2 text-sm font-medium",
            tab === "balances" ? "border-brand-600 bg-brand-50 text-brand-700" : "border-line bg-surface text-ink-2",
          ].join(" ")}
        >
          Bakiyeler
        </button>
      </div>
      {tab === "catalog" ? <Catalog /> : <Balances />}
    </div>
  );
}

function Catalog() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<PackageDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deactivating, setDeactivating] = useState<PackageDto | null>(null);

  const deactivate = useMutation({
    mutationFn: (pkg: PackageDto) =>
      api.company.updatePackage(pkg.id, {
        name: pkg.name,
        description: pkg.description,
        sessionCount: pkg.sessionCount,
        price: pkg.price,
        isActive: false,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.packages(companyId) });
      toastSuccess("Paket pasife alındı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paket güncellenemedi."),
  });

  const reactivate = useMutation({
    mutationFn: (pkg: PackageDto) =>
      api.company.updatePackage(pkg.id, {
        name: pkg.name,
        description: pkg.description,
        sessionCount: pkg.sessionCount,
        price: pkg.price,
        isActive: true,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.packages(companyId) });
      toastSuccess("Paket yeniden aktifleştirildi.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paket güncellenemedi."),
  });

  return (
    <>
      <div className="flex justify-end">
        <Button size="sm" onClick={() => setCreating(true)}>
          Paket oluştur
        </Button>
      </div>
      <CatalogList onEdit={setEditing} onDeactivate={setDeactivating} onReactivate={(pkg) => reactivate.mutate(pkg)} />

      <PackageDialog
        key={editing?.id ?? "new"}
        open={creating || editing !== null}
        existing={editing}
        onClose={() => {
          setCreating(false);
          setEditing(null);
        }}
      />
      <ConfirmDialog
        open={deactivating !== null}
        onClose={() => setDeactivating(null)}
        title="Paket pasife alınsın mı?"
        description={`${deactivating?.name ?? ""} pasife alınır; mevcut bakiyeler etkilenmez.`}
        confirmLabel="Pasife al"
        danger
        pending={deactivate.isPending}
        onConfirm={() => {
          if (deactivating !== null) deactivate.mutate(deactivating);
          setDeactivating(null);
        }}
      />
    </>
  );
}

function CatalogList({
  onEdit,
  onDeactivate,
  onReactivate,
}: {
  readonly onEdit: (pkg: PackageDto) => void;
  readonly onDeactivate: (pkg: PackageDto) => void;
  readonly onReactivate: (pkg: PackageDto) => void;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const packages = useQuery({ queryKey: panelKeys.packages(companyId), queryFn: () => api.company.packages() });

  return (
    <AsyncBoundary state={packages} isEmpty={(packages.data?.length ?? 0) === 0}>
      {(packages.data?.length ?? 0) === 0 ? (
        <EmptyState title="Paket yok" description="İlk ders paketinizi oluşturun." />
      ) : (
        <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {(packages.data ?? []).map((pkg) => (
            <li
              key={pkg.id}
              className={[
                "flex flex-col gap-2 rounded-lg border p-4 transition-all duration-200 hover:-translate-y-0.5",
                pkg.isActive ? "border-line bg-surface shadow-xs hover:shadow-md" : "border-line/60 bg-surface/60 opacity-80",
              ].join(" ")}
            >
              <div className="flex items-start justify-between gap-2">
                <p className="font-semibold text-ink">{pkg.name}</p>
                {pkg.isActive ? <Badge tone="success" dot={false}>Aktif</Badge> : <Badge dot={false}>Pasif</Badge>}
              </div>
              {pkg.description !== null ? <p className="text-[13px] text-ink-2">{pkg.description}</p> : null}
              <p className="font-display mt-1 text-[26px] font-extrabold leading-none tracking-[-0.02em] text-ink tnum">
                {formatPrice(pkg.price)}
              </p>
              <p className="text-xs text-ink-3 tnum">
                {pkg.sessionCount} ders
                <span className="mx-1.5 text-line-strong">·</span>
                ders başı ≈ {formatPrice(Math.round((pkg.price / Math.max(1, pkg.sessionCount)) * 100) / 100)}
              </p>
              <div className="mt-auto flex gap-2 pt-2">
                <Button variant="secondary" size="sm" onClick={() => onEdit(pkg)}>
                  Düzenle
                </Button>
                {pkg.isActive ? (
                  <Button variant="ghost" size="sm" className="text-danger" onClick={() => onDeactivate(pkg)}>
                    Pasife al
                  </Button>
                ) : (
                  <Button variant="ghost" size="sm" className="text-success" onClick={() => onReactivate(pkg)}>
                    Yeniden aktifleştir
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </AsyncBoundary>
  );
}

function PackageDialog({
  open,
  existing,
  onClose,
}: {
  readonly open: boolean;
  readonly existing: PackageDto | null;
  readonly onClose: () => void;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [values, setValues] = useState(() => formFrom(existing));
  const [errors, setErrors] = useState<{ name?: string; sessionCount?: string; price?: string }>({});

  const save = useMutation({
    mutationFn: async () => {
      const parsed = packageSchema.safeParse(values);
      if (!parsed.success) {
        const nextErrors: { name?: string; sessionCount?: string; price?: string } = {};
        for (const issue of parsed.error.issues) {
          const key = issue.path[0];
          if (key === "name" && nextErrors.name === undefined) nextErrors.name = issue.message;
          if (key === "sessionCount" && nextErrors.sessionCount === undefined) nextErrors.sessionCount = issue.message;
          if (key === "price" && nextErrors.price === undefined) nextErrors.price = issue.message;
        }
        setErrors(nextErrors);
        throw new Error("Lütfen işaretli alanları düzeltin.");
      }
      setErrors({});
      const body = {
        name: parsed.data.name,
        description: parsed.data.description === "" ? null : parsed.data.description,
        sessionCount: Number(parsed.data.sessionCount),
        price: Number(parsed.data.price.replace(",", ".")),
        isActive: existing ? existing.isActive : true,
      };
      return existing === null ? api.company.createPackage(body) : api.company.updatePackage(existing.id, body);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.packages(companyId) });
      toastSuccess(existing === null ? "Paket oluşturuldu." : "Paket güncellendi.");
      onClose();
    },
    onError: () => {
      /* field errors are rendered inline; server errors surface via mutation error below */
    },
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={existing === null ? "Paket oluştur" : "Paketi düzenle"}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Vazgeç
          </Button>
          <Button size="sm" pending={save.isPending} onClick={() => save.mutate()}>
            Kaydet
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <FormField label="Ad" required error={errors.name}>
          {(id) => (
            <Input id={id} value={values.name} onChange={(e) => setValues((v) => ({ ...v, name: e.target.value }))} invalid={errors.name !== undefined} />
          )}
        </FormField>
        <FormField label="Açıklama">
          {(id) => <Input id={id} value={values.description} onChange={(e) => setValues((v) => ({ ...v, description: e.target.value }))} />}
        </FormField>
        <div className="grid grid-cols-2 gap-4">
          <FormField label="Ders sayısı" required error={errors.sessionCount}>
            {(id) => (
              <Input
                id={id}
                inputMode="numeric"
                value={values.sessionCount}
                onChange={(e) => setValues((v) => ({ ...v, sessionCount: e.target.value }))}
                invalid={errors.sessionCount !== undefined}
              />
            )}
          </FormField>
          <FormField label="Fiyat (TL)" required error={errors.price}>
            {(id) => (
              <Input
                id={id}
                inputMode="decimal"
                value={values.price}
                onChange={(e) => setValues((v) => ({ ...v, price: e.target.value }))}
                invalid={errors.price !== undefined}
              />
            )}
          </FormField>
        </div>
        {save.isError && errors.name === undefined && errors.sessionCount === undefined && errors.price === undefined ? (
          <p role="alert" className="text-sm font-medium text-danger">
            {save.error instanceof Error ? save.error.message : "Kaydedilemedi."}
          </p>
        ) : null}
      </div>
    </Dialog>
  );
}

const packageSchema = z.object({
  name: z.string().trim().min(1, "Ad zorunludur."),
  description: z.string(),
  sessionCount: z.string().regex(/^\d+$/, "Sayı girin."),
  price: z.string().regex(/^\d+([.,]\d{1,2})?$/, "Geçerli bir fiyat girin."),
});

function formFrom(existing: PackageDto | null): { name: string; description: string; sessionCount: string; price: string } {
  return {
    name: existing?.name ?? "",
    description: existing?.description ?? "",
    sessionCount: existing !== null ? String(existing.sessionCount) : "",
    price: existing !== null ? String(existing.price) : "",
  };
}

function formatPrice(price: number): string {
  return `${price.toLocaleString("tr-TR", { minimumFractionDigits: 0, maximumFractionDigits: 2 })} ₺`;
}

function Balances() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const [search, setSearch] = useState("");
  const balances = useQuery({
    queryKey: panelKeys.packageBalances(companyId),
    queryFn: () => api.company.packageBalances(),
  });

  const filtered = useMemo(() => {
    const rows = [...(balances.data ?? [])].sort((a, b) => a.remainingSessions - b.remainingSessions);
    const needle = search.trim().toLocaleLowerCase("tr");
    if (needle === "") return rows;
    return rows.filter(
      (row) =>
        row.customerName.toLocaleLowerCase("tr").includes(needle) ||
        row.packageName.toLocaleLowerCase("tr").includes(needle),
    );
  }, [balances.data, search]);

  // Group by member so an operator sees one person's whole package wallet at once.
  const grouped = useMemo(() => {
    const map = new Map<string, typeof filtered>();
    for (const row of filtered) {
      const list = map.get(row.customerId) ?? [];
      list.push(row);
      map.set(row.customerId, list);
    }
    return [...map.entries()];
  }, [filtered]);

  return (
    <div className="flex flex-col gap-4">
      <FilterBar search={search} onSearchChange={setSearch} searchPlaceholder="Üye veya paket ara…" />
      <AsyncBoundary state={balances} isEmpty={filtered.length === 0}>
        {filtered.length === 0 ? (
          <EmptyState title="Bakiye yok" description="Üyelere tanımlanan paketler burada görünür." />
        ) : (
          <div className="cb-stagger grid gap-3 md:grid-cols-2">
            {grouped.map(([customerId, memberPackages]) => (
              <section key={customerId} className="rounded-lg border border-line/80 bg-surface p-4 shadow-xs">
                <div className="mb-3 flex items-center gap-2.5">
                  <CrewAvatar name={memberPackages[0]?.customerName ?? "?"} />
                  <p className="truncate text-sm font-semibold text-ink">{memberPackages[0]?.customerName ?? "—"}</p>
                  <Badge tone={memberPackages.some((p) => p.remainingSessions > 0) ? "success" : "neutral"} dot={false} className="ml-auto">
                    {memberPackages.reduce((sum, p) => sum + p.remainingSessions, 0)} ders
                  </Badge>
                </div>
                <ul className="flex flex-col gap-2.5">
                  {memberPackages.map((pkg) => (
                    <li key={pkg.id}>
                      <div className="mb-1 flex items-center justify-between gap-2 text-[13px]">
                        <span className="truncate text-ink-2">{pkg.packageName}</span>
                        <span className={`font-mono font-semibold tnum ${pkg.remainingSessions === 0 ? "text-danger" : "text-ink"}`}>
                          {pkg.remainingSessions}/{pkg.totalSessions}
                        </span>
                      </div>
                      <MeterBar value={pkg.remainingSessions} max={pkg.totalSessions} label={`${pkg.packageName} kalan`} />
                      <p className="mt-1 text-[11px] text-ink-3">Tanımlanma: {formatIsoDateTr(pkg.assignedAtUtc.slice(0, 10))}</p>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </div>
        )}
      </AsyncBoundary>
    </div>
  );
}
