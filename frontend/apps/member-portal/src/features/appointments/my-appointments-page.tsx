import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { MemberAppointmentDto } from "@crewbase/api-types";
import {
  APPOINTMENT_STATUS_LABELS,
  AsyncBoundary,
  Badge,
  Button,
  ConfirmDialog,
  EmptyState,
  PageHeader,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { resolveClubSubdomain } from "../../app/club";
import { formatIsoDateTr, todayIso } from "../../shared/dates";
import { toastError } from "../../shared/toast-store";

export function MyAppointmentsPage() {
  const api = useApi();
  const subdomain = resolveClubSubdomain() ?? "";
  const queryClient = useQueryClient();
  const [cancelTarget, setCancelTarget] = useState<MemberAppointmentDto | null>(null);

  const appointments = useQuery({
    queryKey: ["member", "appointments"],
    queryFn: () => api.member.appointments(),
  });

  const cancel = useMutation({
    mutationFn: (id: string) => api.member.cancelAppointment(id),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["member", "appointments"] }),
        queryClient.invalidateQueries({ queryKey: ["member", "packages"] }),
        queryClient.invalidateQueries({ queryKey: ["public", subdomain, "availability"] }),
      ]);
      setCancelTarget(null);
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Randevu iptal edilemedi."),
  });

  const { upcoming, past } = useMemo(() => splitByDate(appointments.data ?? []), [appointments.data]);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Randevularım" />

      <section aria-label="Yaklaşan randevular">
        <h2 className="mb-2 text-sm font-semibold text-ink">Yaklaşan</h2>
        <AsyncBoundary state={appointments} isEmpty={upcoming.length === 0}>
          {upcoming.length === 0 ? (
            <EmptyState title="Yaklaşan randevu yok" description="Ana sayfadan yeni bir rezervasyon yapabilirsiniz." />
          ) : (
            <ul className="flex flex-col gap-2">
              {upcoming.map((appointment) => (
                <AppointmentRow key={appointment.id} appointment={appointment} onCancel={() => setCancelTarget(appointment)} />
              ))}
            </ul>
          )}
        </AsyncBoundary>
      </section>

      <section aria-label="Geçmiş randevular">
        <h2 className="mb-2 text-sm font-semibold text-ink">Geçmiş</h2>
        <AsyncBoundary state={appointments} isEmpty={past.length === 0}>
          {past.length === 0 ? (
            <EmptyState title="Geçmiş kayıt yok" />
          ) : (
            <ul className="flex flex-col gap-2">
              {past.map((appointment) => (
                <AppointmentRow key={appointment.id} appointment={appointment} />
              ))}
            </ul>
          )}
        </AsyncBoundary>
      </section>

      <ConfirmDialog
        open={cancelTarget !== null}
        onClose={() => setCancelTarget(null)}
        title="Randevuyu iptal et"
        description={`${formatIsoDateTr(cancelTarget?.date ?? "")} ${cancelTarget?.startTime ?? ""} randevunuz iptal edilecek.${cancelTarget?.usedPackage === true ? " Paket dersiniz otomatik iade edilir." : ""}`}
        confirmLabel="İptal et"
        danger
        pending={cancel.isPending}
        onConfirm={() => {
          if (cancelTarget !== null) cancel.mutate(cancelTarget.id);
        }}
      />
    </div>
  );
}

function AppointmentRow({
  appointment,
  onCancel,
}: {
  readonly appointment: MemberAppointmentDto;
  readonly onCancel?: () => void;
}) {
  const upcoming = appointment.date >= todayIso();
  const cancellable =
    onCancel !== undefined &&
    upcoming &&
    appointment.status !== "Cancelled" &&
    appointment.status !== "Completed";

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-1.5 rounded-lg border border-line bg-surface px-4 py-3 shadow-xs">
      <div className="min-w-[120px]">
        <p className="text-sm font-medium text-ink">
          {formatIsoDateTr(appointment.date)} · <span className="font-mono">{appointment.startTime}</span>
        </p>
        <p className="text-xs text-ink-3">{appointment.boatClass}{appointment.boatName !== null ? ` · ${appointment.boatName}` : ""}</p>
      </div>
      {appointment.instructorName !== null ? (
        <span className="text-xs text-ink-2">🧑‍🏫 {appointment.instructorName}</span>
      ) : null}
      {appointment.usedPackage ? <Badge tone="info">Paket</Badge> : null}
      <Badge tone={statusTone(appointment.status)}>{APPOINTMENT_STATUS_LABELS[appointment.status] ?? appointment.status}</Badge>
      {cancellable ? (
        <Button variant="ghost" size="sm" className="ml-auto text-danger" onClick={() => onCancel?.()}>
          İptal et
        </Button>
      ) : null}
      {appointment.crewmates.length > 0 ? (
        <p className="w-full text-xs text-ink-3">Tekne arkadaşları: {appointment.crewmates.join(", ")}</p>
      ) : null}
    </li>
  );
}

function statusTone(status: string): "warning" | "info" | "success" | "danger" | "neutral" {
  if (status === "Confirmed") return "info";
  if (status === "Completed") return "success";
  if (status === "Cancelled") return "danger";
  return "warning";
}

function splitByDate(rows: readonly MemberAppointmentDto[]): {
  upcoming: readonly MemberAppointmentDto[];
  past: readonly MemberAppointmentDto[];
} {
  const today = todayIso();
  return {
    upcoming: rows.filter((r) => r.date >= today),
    past: rows
      .filter((r) => r.date < today)
      .slice()
      .sort((a, b) => (a.date < b.date ? 1 : -1)),
  };
}
