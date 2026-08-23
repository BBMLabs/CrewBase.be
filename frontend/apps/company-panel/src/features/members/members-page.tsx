import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CustomerDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  Button,
  CrewAvatar,
  Drawer,
  EmptyState,
  FilterBar,
  FormField,
  Input,
  MEMBER_LOG_EVENT_LABELS,
  MEMBER_LOG_EVENT_UNLABELED,
  MeterBar,
  PageHeader,
  rowingLevelLabel,
  Select,
  TimelineList,
  TimelineRow,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { formatIsoDateTr } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";
import { createMemberSchema, type CreateMemberForm } from "./member-schemas";

export function MembersPage() {
  const [tab, setTab] = useState<"directory" | "logs">("directory");
  return (
    <div className="flex flex-col gap-5">
      <PageHeader title="Üyeler" description="Kulüp üyeleri, seviyeleri, paketleri ve hareket geçmişleri." />
      <div role="tablist" aria-label="Üyeler görünümleri" className="glass-panel flex gap-1 rounded-2xl p-1">
        <TabButton active={tab === "directory"} onClick={() => setTab("directory")}>
          Üye Dizini
        </TabButton>
        <TabButton active={tab === "logs"} onClick={() => setTab("logs")}>
          Son Hareketler
        </TabButton>
      </div>
      {tab === "directory" ? <Directory /> : <RecentLogs />}
    </div>
  );
}

function TabButton({
  active,
  onClick,
  children,
}: {
  readonly active: boolean;
  readonly onClick: () => void;
  readonly children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={[
        "rounded-xl px-4 py-2 text-sm font-semibold transition-all duration-200",
        active
          ? "bg-brand-500 text-white shadow-[0_12px_30px_-18px_rgba(29,116,201,0.9)]"
          : "text-ink-2 hover:bg-surface-2 hover:text-ink",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function Directory() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const [search, setSearch] = useState("");
  const [levelFilter, setLevelFilter] = useState<string>("all");
  const [creating, setCreating] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const members = useQuery({ queryKey: panelKeys.members.list(companyId), queryFn: () => api.company.members() });

  const filtered = useMemo(() => {
    const rows = members.data ?? [];
    const needle = search.trim().toLocaleLowerCase("tr");
    return rows.filter((m) => {
      if (levelFilter !== "all" && String(m.level) !== levelFilter) return false;
      if (needle === "") return true;
      return (
        m.fullName.toLocaleLowerCase("tr").includes(needle) ||
        m.phone.includes(needle) ||
        (m.email?.toLocaleLowerCase("tr").includes(needle) ?? false)
      );
    });
  }, [members.data, search, levelFilter]);

  const selected = useMemo(
    () => (selectedId === null ? null : (members.data ?? []).find((m) => m.id === selectedId) ?? null),
    [members.data, selectedId],
  );

  return (
    <>
      <FilterBar search={search} onSearchChange={setSearch} searchPlaceholder="İsim, telefon veya e-posta ara…">
        <Select value={levelFilter} onChange={(e) => setLevelFilter(e.target.value)} aria-label="Seviye filtresi" className="h-10 w-auto min-w-[140px]">
          <option value="all">Tüm seviyeler</option>
          {Array.from({ length: 11 }, (_, i) => (
            <option key={i} value={String(i)}>
              Seviye {i}
            </option>
          ))}
        </Select>
        <Button size="md" onClick={() => setCreating(true)}>
          Üye ekle
        </Button>
      </FilterBar>

      <AsyncBoundary state={members} isEmpty={filtered.length === 0}>
        {(members.data?.length ?? 0) === 0 ? (
          <EmptyState title="Henüz üye yok" description="Panelden eklediğiniz üyeler burada listelenir." />
        ) : filtered.length === 0 ? (
          <EmptyState title="Sonuç bulunamadı" description="Arama/filtre kriterlerinize uyan üye yok." />
        ) : (
          <div className="overflow-x-auto rounded-[22px] border border-line/80 bg-surface/75 shadow-[0_16px_36px_-28px_rgba(15,29,43,0.6)] backdrop-blur-sm">
            <table className="w-full min-w-[720px] text-sm">
              <caption className="sr-only">Üye dizini</caption>
              <thead>
                <tr className="border-b border-line/70 bg-surface-2/70 text-left text-[11px] uppercase tracking-[0.07em] text-ink-3">
                  <th scope="col" className="px-4 py-2.5">Üye</th>
                  <th scope="col" className="px-4 py-2.5">Telefon</th>
                  <th scope="col" className="px-4 py-2.5">E-posta</th>
                  <th scope="col" className="px-4 py-2.5">Seviye</th>
                  <th scope="col" className="px-4 py-2.5">Kayıt</th>
                  <th scope="col" className="px-4 py-2.5 text-right">İşlem</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line/60">
                {filtered.map((member) => (
                  <tr key={member.id} className="group transition-colors hover:bg-surface-2/60">
                    <td className="px-4 py-2.5">
                      <span className="flex items-center gap-2.5">
                        <CrewAvatar name={member.fullName} />
                        <span className="font-medium text-ink">{member.fullName}</span>
                      </span>
                    </td>
                    <td className="px-4 py-2.5 font-mono text-[13px] text-ink-2">{member.phone}</td>
                    <td className="px-4 py-2.5 text-ink-2">{member.email ?? "—"}</td>
                    <td className="px-4 py-2.5"><Badge tone="accent">{rowingLevelLabel(member.level)}</Badge></td>
                    <td className="px-4 py-2.5 text-ink-3">{formatIsoDateTr(member.createdAtUtc.slice(0, 10))}</td>
                    <td className="px-4 py-2.5 text-right">
                      <Button variant="ghost" size="sm" onClick={() => setSelectedId(member.id)}>
                        Detay
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </AsyncBoundary>

      <CreateMemberDrawer open={creating} onClose={() => setCreating(false)} />
      {selected !== null ? <MemberDrawer memberId={selected.id} onClose={() => setSelectedId(null)} /> : null}
    </>
  );
}

function CreateMemberDrawer({ open, onClose }: { readonly open: boolean; readonly onClose: () => void }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();

  const [values, setValues] = useState({ fullName: "", phone: "", email: "", level: "0" });
  const [errors, setErrors] = useState<Partial<Record<keyof CreateMemberForm, string>>>({});

  const create = useMutation({
    mutationFn: () => {
      const parsed = createMemberSchema.safeParse(values);
      if (!parsed.success) {
        const nextErrors: Partial<Record<keyof CreateMemberForm, string>> = {};
        for (const issue of parsed.error.issues) {
          const key = issue.path[0];
          if (typeof key === "string") {
            const typedKey = key as keyof CreateMemberForm;
            if (nextErrors[typedKey] === undefined) nextErrors[typedKey] = issue.message;
          }
        }
        setErrors(nextErrors);
        throw new Error("Lütfen işaretli alanları düzeltin.");
      }
      setErrors({});
      return api.company.createMember({
        fullName: parsed.data.fullName,
        phone: parsed.data.phone,
        email: parsed.data.email === "" ? null : parsed.data.email,
        level: Number(parsed.data.level),
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.members.root(companyId) });
      toastSuccess("Üye eklendi.");
      setValues({ fullName: "", phone: "", email: "", level: "0" });
      onClose();
    },
    onError: () => {
      /* inline errors rendered */
    },
  });

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Üye ekle"
      description="Telefonla eklenen üye, aynı telefonla siteye kayıt olduğunda hesabına kavuşur."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>Vazgeç</Button>
          <Button size="sm" pending={create.isPending} onClick={() => create.mutate()}>Kaydet</Button>
        </>
      }
    >
      <form className="flex flex-col gap-4" onSubmit={(e) => { e.preventDefault(); create.mutate(); }} noValidate>
        <FormField label="Ad Soyad" required error={errors.fullName}>
          {(id) => (
            <Input id={id} value={values.fullName} onChange={(e) => setValues((v) => ({ ...v, fullName: e.target.value }))} invalid={errors.fullName !== undefined} />
          )}
        </FormField>
        <FormField label="Telefon" required error={errors.phone}>
          {(id) => (
            <Input id={id} type="tel" value={values.phone} onChange={(e) => setValues((v) => ({ ...v, phone: e.target.value }))} invalid={errors.phone !== undefined} />
          )}
        </FormField>
        <FormField label="E-posta" error={errors.email}>
          {(id) => (
            <Input id={id} type="email" value={values.email} onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))} />
          )}
        </FormField>
        <FormField label="Seviye" required error={errors.level}>
          {(id) => (
            <Select id={id} value={values.level} onChange={(e) => setValues((v) => ({ ...v, level: e.target.value }))}>
              {Array.from({ length: 11 }, (_, i) => (
                <option key={i} value={String(i)}>
                  {i} — {rowingLevelLabel(i)}
                </option>
              ))}
            </Select>
          )}
        </FormField>
        {create.isError && Object.keys(errors).length === 0 ? (
          <p role="alert" className="text-sm text-danger">{create.error instanceof Error ? create.error.message : ""}</p>
        ) : null}
      </form>
    </Drawer>
  );
}

/** Detail drawer with three sections: profile+level / packages / activity timeline. */
function MemberDrawer({ memberId, onClose }: { readonly memberId: string; readonly onClose: () => void }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const members = useQuery({ queryKey: panelKeys.members.list(companyId), queryFn: () => api.company.members() });
  const member = (members.data ?? []).find((m) => m.id === memberId);

  return member === undefined ? null : (
    <MemberDrawerInner member={member} onClose={onClose} />
  );
}

function MemberDrawerInner({ member, onClose }: { readonly member: CustomerDto; readonly onClose: () => void }) {
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"profile" | "packages" | "activity">("profile");

  const invalidateAll = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: panelKeys.members.packages(companyId, member.id) }),
      queryClient.invalidateQueries({ queryKey: panelKeys.members.logs(companyId, member.id, 50) }),
      queryClient.invalidateQueries({ queryKey: panelKeys.packageBalances(companyId) }),
      queryClient.invalidateQueries({ queryKey: panelKeys.members.list(companyId) }),
    ]);
  };

  return (
    <Drawer open onClose={onClose} title={member.fullName} description={`${member.phone}${member.email !== null ? ` · ${member.email}` : ""}`}>
      {/* Tabs */}
      <div role="tablist" aria-label="Üye detayı bölümleri" className="mb-5 flex gap-1 border-b border-line pb-3">
        {([["profile", "Profil"], ["packages", "Paketler"], ["activity", "Hareketler"]] as const).map(([key, label]) => (
          <button
            key={key}
            type="button"
            role="tab"
            aria-selected={tab === key}
            onClick={() => setTab(key)}
            className={[
              "rounded-md px-3 py-1.5 text-[13px] font-semibold transition-colors",
              tab === key ? "bg-brand-50 text-brand-700" : "text-ink-3 hover:bg-surface-2 hover:text-ink-2",
            ].join(" ")}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === "profile" ? (
        <ProfileSection member={member} onChanged={invalidateAll} />
      ) : tab === "packages" ? (
        <PackagesSection member={member} onChanged={invalidateAll} />
      ) : (
        <ActivitySection memberId={member.id} />
      )}
    </Drawer>
  );
}

function ProfileSection({ member, onChanged }: { readonly member: CustomerDto; readonly onChanged: () => Promise<void> }) {
  const api = useApi();
  const setLevel = useMutation({
    mutationFn: (level: number) => api.company.setMemberLevel(member.id, level),
    onSuccess: async (_data, level) => {
      toastSuccess(`Seviye güncellendi: ${rowingLevelLabel(level)}`);
      await onChanged();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Seviye güncellenemedi."),
  });

  return (
    <section aria-label="Profil" className="flex flex-col gap-5">
      <dl className="grid grid-cols-2 gap-x-4 gap-y-2 rounded-lg bg-surface-2/60 p-4 text-[13px]">
        <dt className="text-ink-3">Telefon</dt>
        <dd className="font-mono">{member.phone}</dd>
        <dt className="text-ink-3">E-posta</dt>
        <dd>{member.email ?? "—"}</dd>
        <dt className="text-ink-3">Kayıt</dt>
        <dd>{formatIsoDateTr(member.createdAtUtc.slice(0, 10))}</dd>
      </dl>
      <FormField label="Kürek seviyesi" hint="Yalnızca firma yöneticisi değiştirebilir. Seans gruplaması bu seviyeye göre yapılır.">
        {(id) => (
          <Select
            id={id}
            value={String(member.level)}
            disabled={setLevel.isPending}
            onChange={(e) => setLevel.mutate(Number(e.target.value))}
          >
            {Array.from({ length: 11 }, (_, i) => (
              <option key={i} value={String(i)}>
                {i} — {rowingLevelLabel(i)}
              </option>
            ))}
          </Select>
        )}
      </FormField>
    </section>
  );
}

function PackagesSection({ member, onChanged }: { readonly member: CustomerDto; readonly onChanged: () => Promise<void> }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const packages = useQuery({
    queryKey: panelKeys.members.packages(companyId, member.id),
    queryFn: () => api.company.memberPackages(member.id),
  });
  const catalog = useQuery({ queryKey: panelKeys.packages(companyId), queryFn: () => api.company.packages() });
  const [assignPackageId, setAssignPackageId] = useState("");

  const assign = useMutation({
    mutationFn: () => api.company.assignPackage(member.id, assignPackageId),
    onSuccess: async () => {
      await onChanged();
      setAssignPackageId("");
      toastSuccess("Paket tanımlandı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paket tanımlanamadı."),
  });

  const activeCatalog = (catalog.data ?? []).filter((p) => p.isActive);
  const rows = packages.data ?? [];

  return (
    <section aria-label="Paketler" className="flex flex-col gap-4">
      {rows.length === 0 ? (
        <p className="text-sm text-ink-3">Tanımlı paket yok.</p>
      ) : (
        <ul className="flex flex-col gap-2.5">
          {rows.map((pkg) => (
            <li key={pkg.id} className="rounded-lg border border-line p-3.5">
              <div className="mb-2 flex items-center justify-between gap-2">
                <p className="text-sm font-semibold text-ink">{pkg.packageName}</p>
                <span className="font-mono text-xs text-ink-2 tnum">
                  kalan {pkg.remainingSessions}/{pkg.totalSessions}
                </span>
              </div>
              <MeterBar value={pkg.remainingSessions} max={pkg.totalSessions} label={`${pkg.packageName} kalan ders`} />
            </li>
          ))}
        </ul>
      )}

      <div className="rounded-lg border border-dashed border-line p-3.5">
        <p className="mb-2 text-[13px] font-semibold text-ink">Yeni paket tanımla</p>
        <div className="flex items-end gap-2">
          <div className="flex-1">
            <Select
              value={assignPackageId}
              onChange={(e) => setAssignPackageId(e.target.value)}
              disabled={activeCatalog.length === 0}
              aria-label="Tanımlanacak paket"
            >
              <option value="">Katalogdan seç…</option>
              {activeCatalog.map((pkg) => (
                <option key={pkg.id} value={pkg.id}>
                  {pkg.name} ({pkg.sessionCount} ders · {pkg.price} ₺)
                </option>
              ))}
            </Select>
          </div>
          <Button size="sm" pending={assign.isPending} disabled={assignPackageId === ""} onClick={() => assign.mutate()}>
            Tanımla
          </Button>
        </div>
      </div>
    </section>
  );
}

function ActivitySection({ memberId }: { readonly memberId: string }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const logs = useQuery({
    queryKey: panelKeys.members.logs(companyId, memberId, 50),
    queryFn: () => api.company.memberLogs(memberId, 50),
  });

  if (logs.isPending) return <p className="text-sm text-ink-3">Yükleniyor…</p>;
  if ((logs.data?.length ?? 0) === 0) return <p className="text-sm text-ink-3">Hareket kaydı yok.</p>;

  return (
    <TimelineList>
      {(logs.data ?? []).map((log, index) => (
        <TimelineRow key={`${log.atUtc}-${index}`}>
          <p className="text-[13px]">
            <span className="font-mono text-[11px] text-ink-3">{log.atUtc.slice(0, 16).replace("T", " ")}</span>
            <span className="ml-2 font-semibold text-ink">
              {MEMBER_LOG_EVENT_LABELS[log.event] ?? MEMBER_LOG_EVENT_UNLABELED}
            </span>
          </p>
          {log.details !== null ? <p className="mt-0.5 text-xs text-ink-3">{log.details}</p> : null}
        </TimelineRow>
      ))}
    </TimelineList>
  );
}

function RecentLogs() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const logs = useQuery({
    queryKey: panelKeys.recentLogs(companyId, 100),
    queryFn: () => api.company.recentLogs(100),
  });

  return (
    <AsyncBoundary state={logs} isEmpty={(logs.data?.length ?? 0) === 0}>
      {(logs.data?.length ?? 0) === 0 ? (
        <EmptyState title="Hareket kaydı yok" />
      ) : (
        <TimelineList>
          {(logs.data ?? []).map((log, index) => (
            <TimelineRow key={`${log.customerId}-${log.atUtc}-${index}`} dotTone={log.event.includes("CANCEL") || log.event.includes("DELETED") ? "danger" : log.event.includes("BOOKED") || log.event.includes("ASSIGNED") ? "brand" : "neutral"}>
              <p className="text-[13px]">
                <span className="font-mono text-[11px] text-ink-3 tnum">{log.atUtc.slice(0, 16).replace("T", " ")}</span>
                <span className="ml-2 font-semibold text-ink">{log.customerName}</span>
                <span className="mx-1.5 text-ink-3">·</span>
                <span className="text-ink-2">{MEMBER_LOG_EVENT_LABELS[log.event] ?? log.event}</span>
                {log.details !== null ? <span className="block pl-1 text-xs text-ink-3">{log.details}</span> : null}
              </p>
            </TimelineRow>
          ))}
        </TimelineList>
      )}
    </AsyncBoundary>
  );
}
