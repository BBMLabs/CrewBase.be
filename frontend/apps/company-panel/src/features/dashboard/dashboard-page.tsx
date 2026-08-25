import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { CompanyStatsDto, DailyCountDto, SessionDto } from "@crewbase/api-types";
import {
  APPOINTMENT_STATUS_LABELS,
  AsyncBoundary,
  Badge,
  BOAT_CLASS_LABELS,
  CompassRose,
  PageHeader,
  rowingLevelLabel,
  SectionCard,
  useCountUp,
  WaveDivider,
} from "@crewbase/design-system";
import { Link } from "react-router";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { addDaysIso, formatIsoDateTr, todayIso, weekdayShort } from "../../shared/dates";

/** Live wall clock for the hero card (club-local time, updates every 30s). */
function useClock(): string {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), 30_000);
    return () => clearInterval(timer);
  }, []);
  return `${`${now.getHours()}`.padStart(2, "0")}:${`${now.getMinutes()}`.padStart(2, "0")}`;
}

export function DashboardPage() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const today = todayIso();

  const stats = useQuery({
    queryKey: panelKeys.stats(companyId),
    queryFn: () => api.company.stats(),
  });

  const todaySessions = useQuery({
    queryKey: panelKeys.sessions.day(companyId, today),
    queryFn: () => api.company.sessions(today),
  });

  if (stats.isPending || todaySessions.isPending) {
    return (
      <div className="flex flex-col gap-7">
        <PageHeader title="Genel Bakış" description="Kulübünün bugünkü ve bu ayki durumu." />
        <DashboardSkeleton />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Genel Bakış"
        description="Bugün kulüpte neler oluyor?"
        meta={
          <span className="tnum">
            {formatIsoDateTr(today)} · {weekdayShort(today)}
          </span>
        }
      />

      <TodayProgram date={today} sessions={todaySessions.data ?? []} />

      <AsyncBoundary state={stats}>
        {stats.data !== undefined ? <StatsView stats={stats.data} /> : null}
      </AsyncBoundary>
    </div>
  );
}

/* ── Today's program ────────────────────────────────────── */

function TodayProgram({ date, sessions }: { readonly date: string; readonly sessions: readonly SessionDto[] }) {
  const sorted = [...sessions].sort((a, b) => a.startTime.localeCompare(b.startTime));

  return (
    <SectionCard
      title="Bugünün programı"
      description={`${formatIsoDateTr(date)} ${weekdayShort(date)}`}
      actions={
        <Link to="/program" className="text-[13px] font-semibold text-brand-600 hover:text-brand-700 hover:underline">
          Gün Programı →
        </Link>
      }
      flush
    >
      {sorted.length === 0 ? (
        <p className="px-5 py-8 text-center text-sm text-ink-3">
          Bugün planlanmış bir antrenman yok.
          <span className="mt-1 block text-xs text-ink-3/80">
            Randevu geldiğinde seanslar burada ve Gün Programında görünür.
          </span>
        </p>
      ) : (
        <ul className="divide-y divide-line/70">
          {sorted.map((session) => (
            <li key={session.id} className="flex flex-wrap items-center gap-x-4 gap-y-2 px-5 py-3 transition-colors hover:bg-surface-2/50">
              <span className="w-12 font-mono text-[15px] font-semibold text-ink">{session.startTime}</span>
              <Badge tone="accent">{BOAT_CLASS_LABELS[session.boatClass] ?? session.boatClass}</Badge>
              <span className="hidden max-w-[180px] truncate text-[13px] text-ink-2 sm:inline">
                Seviye: {rowingLevelLabel(session.level)}
              </span>
              <CapacityPill filled={session.memberCount} total={session.capacity} />
              <span className="ml-auto hidden items-center gap-3 text-[13px] text-ink-2 md:flex">
                <span className="max-w-[140px] truncate">
                  {session.instructorName !== null ? `🧑‍🏫 ${session.instructorName}` : <span className="text-warning">eğitmen atanmadı</span>}
                </span>
                <span className="max-w-[120px] truncate">
                  {session.boatName !== null ? `🚣 ${session.boatName}` : <span className="text-warning">tekne atanmadı</span>}
                </span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}

function CapacityPill({ filled, total }: { readonly filled: number; readonly total: number }) {
  const full = filled >= total;
  return (
    <span
      className={[
        "rounded-full px-2.5 py-0.5 font-mono text-xs font-semibold",
        full ? "bg-danger-bg text-danger" : "bg-success-bg text-success",
      ].join(" ")}
      aria-label={`Kontenjan ${filled}/${total}`}
    >
      {filled}/{total}
    </span>
  );
}

/** RowingLevels labels come from design-system; see rowingLevelLabel import above. */

/* ── Skeletons (match final structure, subtle pulse) ────── */

function DashboardSkeleton() {
  return (
    <>
      {/* Program skeleton */}
      <div className="rounded-lg border border-line/80 bg-surface shadow-sm" aria-hidden="true">
        <div className="border-b border-line/70 px-5 py-4">
          <div className="h-3.5 w-36 animate-pulse rounded bg-surface-2" />
        </div>
        {[0, 1, 2].map((row) => (
          <div key={row} className="flex items-center gap-4 border-b border-line/50 px-5 py-3.5 last:border-b-0">
            <div className="h-4 w-12 animate-pulse rounded bg-surface-2" style={{ animationDelay: `${row * 90}ms` }} />
            <div className="h-5 w-14 animate-pulse rounded-full bg-surface-2" style={{ animationDelay: `${row * 90 + 60}ms` }} />
            <div className="h-4 flex-1 animate-pulse rounded bg-surface-2" style={{ animationDelay: `${row * 90 + 120}ms` }} />
          </div>
        ))}
      </div>

      {/* Stat hero pair */}
      <div className="grid gap-4 md:grid-cols-2" aria-hidden="true">
        <div className="h-40 animate-pulse rounded-xl bg-surface-2" />
        <div className="grid grid-cols-2 gap-4">
          {[0, 1, 2, 3].map((card) => (
            <div key={card} className="h-[88px] animate-pulse rounded-lg border border-line/60 bg-surface" style={{ animationDelay: `${card * 80}ms` }} />
          ))}
        </div>
      </div>

      {/* Chart skeleton */}
      <div className="rounded-lg border border-line/80 bg-surface p-5 shadow-sm" aria-hidden="true">
        <div className="mb-4 h-3.5 w-40 animate-pulse rounded bg-surface-2" />
        <div className="flex h-32 items-end gap-3">
          {[38, 62, 45, 80, 55, 30, 66].map((height, index) => (
            <div key={index} className="w-full animate-pulse rounded-t-md bg-surface-2" style={{ height: `${height}%`, animationDelay: `${index * 70}ms` }} />
          ))}
        </div>
      </div>
    </>
  );
}

/* ── Stat cards ─────────────────────────────────────────── */

type StatIconKind = "today" | "month" | "members" | "sessions" | "package" | "credit";

function StatIcon({ kind }: { readonly kind: StatIconKind }) {
  const common = {
    width: 16,
    height: 16,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.9,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": true,
  } as const;
  switch (kind) {
    case "today":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="9" />
          <path d="M12 7v5l3 2" />
        </svg>
      );
    case "sessions":
      return (
        <svg {...common}>
          <rect x="3" y="4.5" width="18" height="17" rx="2.5" />
          <path d="M8 2.5v4M16 2.5v4M3 10h18" />
        </svg>
      );
    case "month":
      return (
        <svg {...common}>
          <path d="M4 20V10M10 20V4M16 20v-8M22 20H2" />
        </svg>
      );
    case "members":
      return (
        <svg {...common}>
          <circle cx="9" cy="8" r="3.5" />
          <path d="M2.5 20c.8-3.2 3.4-5 6.5-5s5.7 1.8 6.5 5M16.5 4.6a3.5 3.5 0 0 1 0 6.8M18 15.2c2 .8 3.2 2.4 3.6 4.8" />
        </svg>
      );
    case "package":
      return (
        <svg {...common}>
          <path d="M21 8.5v9L12 22 3 17.5v-9L12 4z" />
          <path d="M3 8.5 12 13l9-4.5M12 13v9" />
        </svg>
      );
    case "credit":
      return (
        <svg {...common}>
          <circle cx="12" cy="12" r="9" />
          <path d="M12 6v12M15.5 8.5c-.7-1-2-1.5-3.5-1.5-2 0-3.5 1-3.5 2.5S10 12 12 12s3.5.8 3.5 2.5S14 17 12 17c-1.5 0-2.8-.5-3.5-1.5" />
        </svg>
      );
  }
}

/* ── Charts ─────────────────────────────────────────────── */

function WeekBars({ days }: { readonly days: readonly DailyCountDto[] }) {
  const max = Math.max(1, ...days.map((d) => d.appointments));
  const today = todayIso();

  return (
    <div>
      <div className="flex h-36 items-end gap-2 sm:gap-3">
        {days.map((day) => {
          const isToday = day.date === today;
          const heightPct = Math.max(4, Math.round((day.appointments / max) * 100));
          return (
            <div key={day.date} className="flex min-w-0 flex-1 flex-col items-center justify-end gap-2 self-stretch">
              <span className={`text-xs font-bold tnum ${isToday ? "text-oar-700" : "text-ink-2"}`}>
                {day.appointments}
              </span>
              <div
                role="img"
                aria-label={`${formatIsoDateTr(day.date)}: ${day.appointments} randevu`}
                style={{ height: `${heightPct}%` }}
                className={[
                  "cb-grow-y w-full max-w-10 rounded-t-md transition-all duration-300 ease-out",
                  isToday
                    ? "bg-linear-to-t from-oar-600 to-oar-500 shadow-sm"
                    : day.appointments === 0
                      ? "bg-surface-2"
                      : "bg-linear-to-t from-brand-600 to-brand-400",
                ].join(" ")}
              />
            </div>
          );
        })}
      </div>
      <div className="mt-2 flex gap-2 sm:gap-3">
        {days.map((day) => {
          const isToday = day.date === today;
          return (
            <div key={day.date} className="flex min-w-0 flex-1 flex-col items-center">
              <span className={`text-[11px] font-semibold ${isToday ? "text-oar-700" : "text-ink-3"}`}>
                {weekdayShort(day.date)}
              </span>
              <span className="font-mono text-[10px] text-ink-3/70">{day.date.slice(8)}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function StatusSegmentedBar({ byStatus, total }: { readonly byStatus: Readonly<Record<string, number>>; readonly total: number }) {
  const order = ["Pending", "Confirmed", "Completed", "Cancelled"] as const;
  const segments = order
    .map((status) => ({ status, count: byStatus[status] ?? 0 }))
    .filter((s) => s.count > 0);

  if (total === 0 || segments.length === 0) {
    return <p className="text-sm text-ink-3">Bu ay henüz randevu yok.</p>;
  }

  return (
    <div>
      <div
        role="img"
        aria-label={`Bu ay ${total} randevu dağılımı`}
        className="flex h-3 w-full overflow-hidden rounded-full bg-surface-2"
      >
        {segments.map((segment) => (
          <div
            key={segment.status}
            style={{ width: `${(segment.count / total) * 100}%` }}
            className={segmentBg(segment.status)}
            title={`${APPOINTMENT_STATUS_LABELS[segment.status] ?? segment.status}: ${segment.count}`}
          />
        ))}
      </div>
      <ul className="mt-4 flex flex-wrap gap-x-6 gap-y-2">
        {segments.map((segment) => (
          <li key={segment.status} className="flex items-center gap-2 text-sm">
            <span aria-hidden="true" className={`size-2 rounded-full ${dotBg(segment.status)}`} />
            <span className="text-ink-2">{APPOINTMENT_STATUS_LABELS[segment.status] ?? segment.status}</span>
            <span className="font-semibold text-ink tnum">{segment.count}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function segmentBg(status: string): string {
  switch (status) {
    case "Pending":
      return "bg-warning/80";
    case "Confirmed":
      return "bg-brand-500";
    case "Completed":
      return "bg-success";
    default:
      return "bg-danger/60";
  }
}

function dotBg(status: string): string {
  switch (status) {
    case "Pending":
      return "bg-warning";
    case "Confirmed":
      return "bg-brand-500";
    case "Completed":
      return "bg-success";
    default:
      return "bg-danger";
  }
}

/* ── Composition ────────────────────────────────────────── */

function StatsView({ stats }: { readonly stats: CompanyStatsDto }) {
  const monthTotal = Object.values(stats.thisMonthByStatus).reduce((sum, count) => sum + count, 0);
  const today = todayIso();
  const clock = useClock();
  const heroCount = useCountUp(stats.appointmentsToday);

  return (
    <div className="flex flex-col gap-6">
      {/* Hero stat pair */}
      <section aria-label="Bugün" className="grid gap-4 md:grid-cols-[1.3fr_1fr]">
        <div className="relative overflow-hidden rounded-[26px] border border-brand-500/20 bg-linear-to-br from-rail via-rail to-[#123858] p-6 shadow-[0_18px_40px_-20px_rgba(12,33,52,0.7)]">
          <CompassRose className="absolute -right-8 -top-10 size-44 text-white/[0.07]" />
          <div
            aria-hidden="true"
            className="pointer-events-none absolute -right-10 -top-14 size-44 rounded-full opacity-25"
            style={{ background: "radial-gradient(circle at center, #4090c4 0%, transparent 65%)" }}
          />
          <div className="relative flex items-start justify-between gap-3">
            <p className="text-[11px] font-bold uppercase tracking-[0.09em] text-rail-text">
              Günlük seyir defteri
            </p>
            <p className="font-mono text-sm font-semibold text-rail-text tnum" aria-label="Yerel saat">
              {clock}
            </p>
          </div>
          <p className="font-display relative mt-3 text-[46px] font-extrabold leading-none text-white tnum">
            {heroCount}
          </p>
          <p className="relative mt-2 max-w-md text-[13px] leading-relaxed text-rail-text">
            Bugün {stats.appointmentsToday} randevu · {stats.sessionsToday} seans suda.
            <br />
            Bu ay toplam {stats.appointmentsThisMonth} randevu.
          </p>
          <div aria-hidden="true" className="relative -mx-6 -mb-6 mt-4 overflow-hidden opacity-30">
            <div className="wave-drift flex w-[200%] text-brand-300/50" style={{ ["--wave-duration" as string]: "18s" }}>
              <svg className="h-10 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none">
                <path d="M0 20 C 90 6, 180 34, 300 20 S 480 6, 600 20 L 600 40 L 0 40 Z" fill="currentColor" />
              </svg>
              <svg className="h-10 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none">
                <path d="M0 20 C 90 6, 180 34, 300 20 S 480 6, 600 20 L 600 40 L 0 40 Z" fill="currentColor" />
              </svg>
            </div>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <MiniStat label="Üye" value={stats.memberCount} hint={`${stats.membersWithAccount} hesaplı`} icon="members" />
          <MiniStat label="Aktif Paket" value={stats.activePackageBalances} hint="bakiyesi olan üye" icon="package" />
          <MiniStat label="Kalan Ders" value={stats.totalRemainingSessions} hint="tüm paketler" tone icon="credit" />
          <MiniStat label="Seans (bugün)" value={stats.sessionsToday} hint="planlanan gruplar" icon="sessions" />
        </div>
      </section>

      <WaveDivider />

      <SectionCard
        title="Önümüzdeki 7 gün"
        description="İptal edilenler hariç randevu sayısı"
        actions={<span className="text-xs text-ink-3 tnum">{formatIsoDateTr(today)} – {formatIsoDateTr(addDaysIso(today, 6))}</span>}
      >
        <WeekBars days={stats.next7Days} />
      </SectionCard>

      <SectionCard title="Bu ay durum dağılımı" description={`Toplam ${monthTotal} randevu`}>
        <StatusSegmentedBar byStatus={stats.thisMonthByStatus} total={monthTotal} />
      </SectionCard>
    </div>
  );
}

function MiniStat({
  label,
  value,
  hint,
  tone,
  icon,
}: {
  readonly label: string;
  readonly value: number;
  readonly hint?: string;
  readonly tone?: boolean;
  readonly icon: StatIconKind;
}) {
  const counted = useCountUp(value);
  return (
    <div className="flex flex-col justify-between rounded-2xl border border-line/80 bg-gradient-to-b from-surface to-surface-2/50 p-3.5 shadow-[0_12px_30px_-20px_rgba(15,29,43,0.55)] transition-all duration-200 hover:-translate-y-0.5 hover:shadow-[0_18px_36px_-18px_rgba(15,29,43,0.48)]">
      <div className="flex items-center justify-between gap-2">
        <p className="text-[11px] font-bold uppercase tracking-[0.07em] text-ink-3">{label}</p>
        <span className={`flex size-7 items-center justify-center rounded-lg ${TONE_CHIP[icon]}`}>
          <StatIcon kind={icon} />
        </span>
      </div>
      <p className={`font-display mt-1.5 text-[26px] font-extrabold leading-none tnum ${tone === true ? "text-brand-600" : "text-ink"}`}>
        {counted}
      </p>
      <p className="mt-1 truncate text-[11px] text-ink-3">{hint ?? ""}</p>
    </div>
  );
}

const TONE_CHIP: Record<StatIconKind, string> = {
  today: "bg-brand-50 text-brand-600",
  sessions: "bg-oar-100 text-oar-600",
  month: "bg-surface-2 text-ink-2",
  members: "bg-success-bg text-success",
  package: "bg-brand-50 text-brand-600",
  credit: "bg-oar-100 text-oar-600",
};
