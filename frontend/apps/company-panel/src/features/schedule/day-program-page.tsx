import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { AppointmentDto, AppointmentStatus, SessionDto } from "@crewbase/api-types";
import {
  APPOINTMENT_STATUS_LABELS,
  AsyncBoundary,
  Badge,
  BOAT_CLASS_LABELS,
  Button,
  CrewAvatar,
  Dialog,
  Drawer,
  EmptyState,
  FormField,
  MeterBar,
  PageHeader,
  rowingLevelLabel,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { addDaysIso, formatIsoDateTr, todayIso, weekdayShort } from "../../shared/dates";
import { toastError } from "../../shared/toast-store";

const ALL_STATUSES: readonly AppointmentStatus[] = ["Pending", "Confirmed", "Cancelled", "Completed"];

const STATUS_RANK: Record<AppointmentStatus, number> = {
  Pending: 0,
  Confirmed: 1,
  Completed: 2,
  Cancelled: 3,
};

export function DayProgramPage() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const [selectedDate, setSelectedDate] = useState(todayIso());

  const stripDays = useMemo(() => Array.from({ length: 7 }, (_, i) => addDaysIso(todayIso(), i - 1)), []);

  const appointments = useQuery({
    queryKey: panelKeys.appointments.list(companyId, selectedDate),
    queryFn: () => api.company.appointments(selectedDate),
  });

  const sessions = useQuery({
    queryKey: panelKeys.sessions.day(companyId, selectedDate),
    queryFn: () => api.company.sessions(selectedDate),
  });

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        title="Gün Programı"
        description={`${formatIsoDateTr(selectedDate)} ${weekdayShort(selectedDate)} — randevular ve seans atamaları`}
        actions={
          <div className="flex items-center gap-2">
            <Button variant="secondary" size="sm" onClick={() => setSelectedDate(addDaysIso(selectedDate, -1))}>
              ‹ Önceki
            </Button>
            <label className="flex items-center gap-2 text-sm text-ink-2">
              <input
                type="date"
                value={selectedDate}
                onChange={(e) => {
                  if (e.target.value !== "") setSelectedDate(e.target.value);
                }}
                aria-label="Tarih seç"
                className="h-8 rounded-sm border border-line bg-surface px-2 text-sm"
              />
            </label>
            <Button variant="secondary" size="sm" onClick={() => setSelectedDate(addDaysIso(selectedDate, 1))}>
              Sonraki ›
            </Button>
          </div>
        }
      />

      <div role="tablist" aria-label="Gün seçimi" className="flex gap-1 overflow-x-auto pb-1">
        {stripDays.map((day) => (
          <button
            key={day}
            type="button"
            role="tab"
            aria-selected={day === selectedDate}
            onClick={() => setSelectedDate(day)}
            className={[
              "flex min-w-[72px] flex-col items-center rounded-md border px-3 py-2 text-xs transition-colors",
              day === selectedDate
                ? "border-brand-600 bg-brand-600 text-white shadow-xs"
                : "border-line bg-surface text-ink-2 hover:border-line-strong",
            ].join(" ")}
          >
            <span>{weekdayShort(day)}</span>
            <span className="font-mono text-[11px] opacity-80">{day.slice(5)}</span>
          </button>
        ))}
      </div>

      <div className="grid items-start gap-6 xl:grid-cols-[3fr_2fr]">
        <section aria-label="Randevular" className="flex flex-col gap-2">
          <AsyncBoundary
            state={appointments}
            isEmpty={(appointments.data?.length ?? 0) === 0}
            emptyState={<EmptyState title="Bu günde randevu yok" description="Seçilen tarihe ait randevu bulunmuyor." />}
          >
            {appointments.data !== undefined ? <TriageList date={selectedDate} rows={appointments.data} /> : null}
          </AsyncBoundary>
        </section>

        <section aria-label="Seanslar" className="flex flex-col gap-3">
          <AsyncBoundary
            state={sessions}
            isEmpty={(sessions.data?.length ?? 0) === 0}
            emptyState={
              <EmptyState title="Bu günde seans yok" description="Randevu alındığında seanslar otomatik oluşur." />
            }
          >
            {sessions.data !== undefined ? (
              <div className="cb-stagger flex flex-col gap-3">
                {[...sessions.data]
                  .sort((a, b) => a.startTime.localeCompare(b.startTime))
                  .map((session) => (
                    <SessionCard key={session.id} session={session} />
                  ))}
              </div>
            ) : null}
          </AsyncBoundary>
        </section>
      </div>
    </div>
  );
}

function statusTone(status: string): "warning" | "info" | "success" | "danger" {
  if (status === "Confirmed") return "info";
  if (status === "Completed") return "success";
  if (status === "Cancelled") return "danger";
  return "warning";
}

/* ── Triage list ────────────────────────────────────────── */

function TriageList({ date, rows }: { readonly date: string; readonly rows: readonly AppointmentDto[] }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [confirmCancel, setConfirmCancel] = useState<AppointmentDto | null>(null);

  const sorted = useMemo(
    () =>
      [...rows].sort((a, b) => {
        const byRank = STATUS_RANK[a.status] - STATUS_RANK[b.status];
        if (byRank !== 0) return byRank;
        if (a.status !== b.status) return a.status.localeCompare(b.status);
        return a.startTime.localeCompare(b.startTime);
      }),
    [rows],
  );

  const setStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: AppointmentStatus }) =>
      api.company.setAppointmentStatus(id, status),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: panelKeys.appointments.root(companyId) }),
        queryClient.invalidateQueries({ queryKey: panelKeys.sessions.day(companyId, date) }),
        queryClient.invalidateQueries({ queryKey: panelKeys.stats(companyId) }),
      ]);
      setConfirmCancel(null);
    },
    onError: (error) => {
      toastError(error instanceof Error ? error.message : "Durum güncellenemedi.");
    },
  });

  const pendingCount = rows.filter((r) => r.status === "Pending").length;

  return (
    <>
      <p className="text-[13px] text-ink-3">
        {pendingCount > 0 ? (
          <span>
            <span className="font-semibold text-warning">{pendingCount} onay bekleyen</span> talep var
          </span>
        ) : (
          "Bekleyen talep yok"
        )}
      </p>

      <ul className="cb-stagger flex flex-col gap-2">
        {sorted.map((row) => {
          const busy = setStatus.isPending && setStatus.variables?.id === row.id;
          return (
            <li
              key={row.id}
              className={[
                "rounded-lg border bg-surface p-3.5 shadow-xs transition-all duration-200",
                row.status === "Pending"
                  ? "border-warning/40 hover:shadow-sm"
                  : "border-line/80 hover:border-line-strong",
              ].join(" ")}
            >
              <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
                <span className="w-12 font-mono text-[15px] font-bold text-ink">{row.startTime}</span>
                <CrewAvatar name={row.customerName} size="sm" />
                <div className="min-w-[140px] flex-1">
                  <p className="text-sm font-semibold text-ink">{row.customerName}</p>
                  <p className="text-xs text-ink-3 tnum">{row.customerPhone}</p>
                </div>
                <Badge tone="accent">{row.boatClass}</Badge>
                <Badge tone={statusTone(row.status)}>{APPOINTMENT_STATUS_LABELS[row.status] ?? row.status}</Badge>
              </div>

              {row.note !== null ? (
                <p className="mt-1.5 truncate pl-[60px] text-xs italic text-ink-3">“{row.note}”</p>
              ) : null}

              <div className="mt-2.5 flex items-center justify-between gap-2 pl-[60px]">
                {row.status === "Pending" ? (
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      pending={busy}
                      onClick={() => setStatus.mutate({ id: row.id, status: "Confirmed" })}
                    >
                      Onayla
                    </Button>
                    <Button variant="ghost" size="sm" className="text-danger" disabled={busy} onClick={() => setConfirmCancel(row)}>
                      İptal
                    </Button>
                    <select
                      disabled={busy}
                      value={row.status}
                      onChange={(e) => {
                        const next = e.target.value as AppointmentStatus;
                        if (next !== "Pending") {
                          if (next === "Cancelled") setConfirmCancel(row);
                          else setStatus.mutate({ id: row.id, status: next });
                        }
                      }}
                      aria-label={`${row.customerName} durumu değiştir`}
                      className="ml-auto h-8 self-center rounded-sm border border-line bg-surface px-2 text-xs text-ink-3"
                    >
                      {ALL_STATUSES.map((value) => (
                        <option key={value} value={value}>
                          {APPOINTMENT_STATUS_LABELS[value] ?? value} olarak işaretle
                        </option>
                      ))}
                    </select>
                  </div>
                ) : row.status === "Confirmed" || row.status === "Cancelled" ? (
                  <div className="flex items-center gap-2">
                    {row.status === "Confirmed" ? (
                      <Button
                        variant="secondary"
                        size="sm"
                        pending={busy}
                        onClick={() => setStatus.mutate({ id: row.id, status: "Completed" })}
                      >
                        Tamamlandı işaretle
                      </Button>
                    ) : null}
                    {row.status === "Cancelled" ? (
                      <Button
                        variant="ghost"
                        size="sm"
                        pending={busy}
                        onClick={() => setStatus.mutate({ id: row.id, status: "Pending" })}
                      >
                        Beklemeye al
                      </Button>
                    ) : null}
                    <span className="ml-auto text-xs text-ink-3">
                      {rowingLevelLabel(row.customerLevel)} · {row.customerEmail ?? "e-posta yok"}
                    </span>
                  </div>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>

      <Dialog
        open={confirmCancel !== null}
        onClose={() => setConfirmCancel(null)}
        title="Randevuyu iptal et"
        size="sm"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setConfirmCancel(null)}>
              Vazgeç
            </Button>
            <Button
              variant="danger"
              size="sm"
              pending={setStatus.isPending}
              onClick={() => {
                if (confirmCancel !== null) setStatus.mutate({ id: confirmCancel.id, status: "Cancelled" });
              }}
            >
              İptal et
            </Button>
          </>
        }
      >
        <p className="text-sm text-ink-2">
          {confirmCancel?.customerName ?? ""} adlı üyenin {formatIsoDateTr(date)} {confirmCancel?.startTime ?? ""}{" "}
          randevusu iptal edilecek.
        </p>
        <p className="mt-2 text-xs text-ink-3">
          Paket kullanılarak alınmışsa ders otomatik iade edilir; iptalden geri açılırsa yeniden düşülür.
        </p>
      </Dialog>
    </>
  );
}

/* ── Session crew cards ─────────────────────────────────── */

function SessionCard({ session }: { readonly session: SessionDto }) {
  const [assignOpen, setAssignOpen] = useState(false);
  const full = session.memberCount >= session.capacity;

  return (
    <article className="rounded-lg border border-line/80 bg-surface shadow-xs transition-shadow duration-200 hover:shadow-md">
      <header className="flex items-center justify-between gap-3 border-b border-line/70 px-4 py-3">
        <div>
          <p className="text-sm font-bold text-ink">
            {session.startTime} · {BOAT_CLASS_LABELS[session.boatClass] ?? session.boatClass}
          </p>
          <p className="text-xs text-ink-3">Seviye: {rowingLevelLabel(session.level)}</p>
        </div>
        <div className="w-24 text-right">
          <span
            className={[
              "block font-mono text-xs font-semibold",
              full ? "text-danger" : "text-success",
            ].join(" ")}
          >
            {session.memberCount}/{session.capacity} kürekçi
          </span>
          <MeterBar value={session.memberCount} max={session.capacity} label="Kontenjan" className="mt-1" />
        </div>
      </header>

      <div className="px-4 py-3">
        <dl className="mb-3 grid grid-cols-2 gap-2 rounded-md bg-surface-2/60 px-3 py-2 text-[13px]">
          <div>
            <dt className="text-[11px] uppercase tracking-wide text-ink-3">Tekne</dt>
            <dd className={session.boatName === null ? "font-medium text-warning" : "font-medium text-ink"}>
              {session.boatName ?? "atanmadı"}
            </dd>
          </div>
          <div>
            <dt className="text-[11px] uppercase tracking-wide text-ink-3">Eğitmen</dt>
            <dd className={session.instructorName === null ? "font-medium text-warning" : "font-medium text-ink"}>
              {session.instructorName ?? "atanmadı"}
            </dd>
          </div>
        </dl>

        {session.members.length > 0 ? (
          <ul className="mb-3 flex flex-wrap gap-1.5">
            {session.members.map((member) => (
              <li
                key={member.appointmentId}
                title={`${member.fullName} · ${rowingLevelLabel(member.level)}`}
                className="flex items-center gap-1.5 rounded-full border border-line bg-surface px-1 pr-2.5 py-0.5 text-xs text-ink-2"
              >
                <CrewAvatar name={member.fullName} size="sm" />
                <span className="max-w-[120px] truncate">{member.fullName}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="mb-3 text-xs text-ink-3">Bu seansta henüz kürekçi yok.</p>
        )}

        <Button variant="secondary" size="sm" onClick={() => setAssignOpen(true)}>
          Tekne / eğitmen ata
        </Button>
      </div>
      <SessionAssignDrawer open={assignOpen} onClose={() => setAssignOpen(false)} session={session} />
    </article>
  );
}

function SessionAssignDrawer({
  open,
  onClose,
  session,
}: {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly session: SessionDto;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [boatId, setBoatId] = useState("");
  const [instructorId, setInstructorId] = useState("");

  const boats = useQuery({ queryKey: panelKeys.boats(companyId), queryFn: () => api.company.boats(), enabled: open });
  const instructors = useQuery({
    queryKey: panelKeys.instructors(companyId),
    queryFn: () => api.company.instructors(),
    enabled: open,
  });

  const assign = useMutation({
    mutationFn: () =>
      api.company.assignSession(session.id, {
        boatId: boatId === "" ? null : boatId,
        instructorId: instructorId === "" ? null : instructorId,
      }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: panelKeys.sessions.root(companyId) }),
        queryClient.invalidateQueries({ queryKey: panelKeys.appointments.root(companyId) }),
      ]);
      onClose();
    },
  });

  const activeBoats = (boats.data ?? []).filter((b) => b.isActive);
  const activeInstructors = (instructors.data ?? []).filter((i) => i.isActive);

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Seans ataması"
      description={`${formatIsoDateTr(session.date)} · ${session.startTime} · ${BOAT_CLASS_LABELS[session.boatClass] ?? session.boatClass}`}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Vazgeç
          </Button>
          <Button
            size="sm"
            pending={assign.isPending}
            onClick={() => assign.mutate()}
            disabled={boatId === "" && instructorId === ""}
          >
            Atamayı kaydet
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-5">
        <p className="rounded-md bg-info-bg px-3 py-2 text-[13px] leading-relaxed text-info">
          Aynı saatte başka bir seansta kullanılan tekne veya görevli eğitmen seçilirse sunucu isteği
          reddeder (<span className="font-mono text-xs">boat_taken</span> /{" "}
          <span className="font-mono text-xs">instructor_busy</span>).
        </p>

        <FormField label={`Tekne (${activeBoats.length} aktif)`} hint="Boş bırakırsanız mevcut atama korunur.">
          {(id) => (
            <select
              id={id}
              value={boatId}
              onChange={(e) => setBoatId(e.target.value)}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm"
            >
              <option value="">— değiştirme —</option>
              {activeBoats.map((boat) => (
                <option key={boat.id} value={boat.id}>
                  {boat.name} ({boat.class} · {boat.capacity} kişi)
                </option>
              ))}
            </select>
          )}
        </FormField>

        <FormField label={`Eğitmen (${activeInstructors.length} aktif)`}>
          {(id) => (
            <select
              id={id}
              value={instructorId}
              onChange={(e) => setInstructorId(e.target.value)}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm"
            >
              <option value="">— değiştirme —</option>
              {activeInstructors.map((instructor) => (
                <option key={instructor.id} value={instructor.id}>
                  {instructor.fullName}
                </option>
              ))}
            </select>
          )}
        </FormField>

        {assign.isError ? (
          <p role="alert" className="rounded-md bg-danger-bg px-3 py-2 text-sm text-danger">
            {assign.error instanceof Error ? assign.error.message : "Atama yapılamadı."}
          </p>
        ) : null}
      </div>
    </Drawer>
  );
}
