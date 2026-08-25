/** Date helpers — the backend exchanges local wall-clock strings (yyyy-MM-dd, HH:mm).
 *  No timezone conversion happens here; `todayIso` uses the browser's local date,
 *  matching how club operations think about "today". */

export function todayIso(): string {
  return toIso(new Date());
}

export function toIso(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function addDaysIso(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map((part) => Number(part));
  if (year === undefined || month === undefined || day === undefined) return iso;
  const date = new Date(year, month - 1, day);
  date.setDate(date.getDate() + days);
  return toIso(date);
}

/** Formats yyyy-MM-dd as a short Turkish display date without shifting timezones. */
export function formatIsoDateTr(iso: string): string {
  const [year, month, day] = iso.split("-");
  if (year === undefined || month === undefined || day === undefined) return iso;
  return `${day}.${month}.${year}`;
}

const TR_WEEKDAY_SHORT = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"] as const;

export function weekdayShort(iso: string): string {
  const [year, month, day] = iso.split("-").map((part) => Number(part));
  if (year === undefined || month === undefined || day === undefined) return "";
  return TR_WEEKDAY_SHORT[new Date(year, month - 1, day).getDay()] ?? "";
}
