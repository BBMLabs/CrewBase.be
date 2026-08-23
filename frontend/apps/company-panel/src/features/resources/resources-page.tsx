import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { BoatDto, InstructorDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  BOAT_CLASS_LABELS,
  Button,
  ConfirmDialog,
  Dialog,
  EmptyState,
  FormField,
  Input,
  PageHeader,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { toastError, toastSuccess } from "../../shared/toast-store";

export function ResourcesPage() {
  const [tab, setTab] = useState<"boats" | "instructors">("boats");
  return (
    <div className="flex flex-col gap-5">
      <PageHeader title="Kaynaklar" description="Tekneler ve eğitmenler; seans atamalarında kullanılır." />
      <div role="tablist" aria-label="Kaynak türü" className="flex gap-1">
        <button
          type="button"
          role="tab"
          aria-selected={tab === "boats"}
          onClick={() => setTab("boats")}
          className={[
            "rounded-md border px-4 py-2 text-sm font-medium",
            tab === "boats" ? "border-brand-600 bg-brand-50 text-brand-700" : "border-line bg-surface text-ink-2",
          ].join(" ")}
        >
          Tekneler
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === "instructors"}
          onClick={() => setTab("instructors")}
          className={[
            "rounded-md border px-4 py-2 text-sm font-medium",
            tab === "instructors" ? "border-brand-600 bg-brand-50 text-brand-700" : "border-line bg-surface text-ink-2",
          ].join(" ")}
        >
          Eğitmenler
        </button>
      </div>
      {tab === "boats" ? <BoatsTab /> : <InstructorsTab />}
    </div>
  );
}

function BoatsTab() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<BoatDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deactivating, setDeactivating] = useState<BoatDto | null>(null);
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "passive">("all");

  const boats = useQuery({ queryKey: panelKeys.boats(companyId), queryFn: () => api.company.boats() });
  const visibleBoats = (boats.data ?? []).filter((b) =>
    statusFilter === "all" ? true : statusFilter === "active" ? b.isActive : !b.isActive,
  );

  const deactivate = useMutation({
    mutationFn: (boat: BoatDto) => api.company.updateBoat(boat.id, { name: boat.name, boatClass: boat.class, isActive: false }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.boats(companyId) });
      toastSuccess("Tekne pasife alındı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Tekne güncellenemedi."),
  });

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div role="group" aria-label="Durum filtresi" className="flex rounded-md border border-line bg-surface p-0.5">
          {(["all", "active", "passive"] as const).map((value) => (
            <button
              key={value}
              type="button"
              aria-pressed={statusFilter === value}
              onClick={() => setStatusFilter(value)}
              className={[
                "rounded-[9px] px-3 py-1.5 text-[13px] font-medium transition-colors",
                statusFilter === value ? "bg-brand-600 text-white" : "text-ink-2 hover:text-ink",
              ].join(" ")}
            >
              {value === "all" ? "Tümü" : value === "active" ? "Aktif" : "Pasif"}
            </button>
          ))}
        </div>
        <Button size="sm" onClick={() => setCreating(true)}>
          Tekne ekle
        </Button>
      </div>
      <AsyncBoundary state={boats} isEmpty={visibleBoats.length === 0}>
        {visibleBoats.length === 0 ? (
          <EmptyState title="Tekne yok" description="1x, 2x veya 4x sınıfında tekne tanımlayın." />
        ) : (
          <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {visibleBoats.map((boat) => (
              <li key={boat.id} className="flex flex-col gap-2 rounded-lg border border-line bg-surface p-4 shadow-xs">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium text-ink">{boat.name}</p>
                  {boat.isActive ? <Badge tone="success">Aktif</Badge> : <Badge>Pasif</Badge>}
                </div>
                <p className="text-[13px] text-ink-2">
                  {BOAT_CLASS_LABELS[boat.class]} · kapasite {boat.capacity}
                </p>
                <div className="mt-auto flex gap-2 pt-2">
                  <Button variant="secondary" size="sm" onClick={() => setEditing(boat)}>
                    Düzenle
                  </Button>
                  {boat.isActive ? (
                    <Button variant="ghost" size="sm" className="text-danger" onClick={() => setDeactivating(boat)}>
                      Pasife al
                    </Button>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        )}
      </AsyncBoundary>

      <BoatDialog key={editing?.id ?? "new"} open={creating || editing !== null} existing={editing} onClose={() => { setCreating(false); setEditing(null); }} />
      <ConfirmDialog
        open={deactivating !== null}
        onClose={() => setDeactivating(null)}
        title="Tekne pasife alınsın mı?"
        description={`${deactivating?.name ?? ""} artık yeni seans atamalarında sunulmaz.`}
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

function BoatDialog({
  open,
  existing,
  onClose,
}: {
  readonly open: boolean;
  readonly existing: BoatDto | null;
  readonly onClose: () => void;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [name, setName] = useState(existing?.name ?? "");
  const [boatClass, setBoatClass] = useState(existing?.class ?? "1x");

  const save = useMutation({
    mutationFn: () => {
      const trimmed = name.trim();
      if (trimmed === "") throw new Error("Tekne adı boş olamaz.");
      return existing === null
        ? api.company.createBoat({ name: trimmed, boatClass })
        : api.company.updateBoat(existing.id, { name: trimmed, boatClass, isActive: existing.isActive });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.boats(companyId) });
      toastSuccess(existing === null ? "Tekne eklendi." : "Tekne güncellendi.");
      onClose();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Kaydedilemedi."),
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={existing === null ? "Tekne ekle" : "Tekneyi düzenle"}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>Vazgeç</Button>
          <Button size="sm" pending={save.isPending} onClick={() => save.mutate()}>Kaydet</Button>
        </>
      }
    >
      <form className="flex flex-col gap-4" onSubmit={(e) => { e.preventDefault(); save.mutate(); }} noValidate>
        <FormField label="Ad" required>
          {(id) => (
            <Input
              id={id}
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Sınıf" required>
          {(id) => (
            <select
              id={id}
              value={boatClass}
              onChange={(e) => setBoatClass(e.target.value as typeof boatClass)}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm"
            >
              <option value="1x">1x — tek kişilik</option>
              <option value="2x">2x — iki kişilik</option>
              <option value="4x">4x — dört kişilik</option>
            </select>
          )}
        </FormField>
      </form>
    </Dialog>
  );
}

function InstructorsTab() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<InstructorDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [deactivating, setDeactivating] = useState<InstructorDto | null>(null);
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "passive">("all");

  const instructors = useQuery({ queryKey: panelKeys.instructors(companyId), queryFn: () => api.company.instructors() });
  const visibleInstructors = (instructors.data ?? []).filter((i) =>
    statusFilter === "all" ? true : statusFilter === "active" ? i.isActive : !i.isActive,
  );

  const deactivate = useMutation({
    mutationFn: (instructor: InstructorDto) =>
      api.company.updateInstructor(instructor.id, {
        fullName: instructor.fullName,
        phone: instructor.phone,
        email: instructor.email,
        isActive: false,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.instructors(companyId) });
      toastSuccess("Eğitmen pasife alındı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Eğitmen güncellenemedi."),
  });

  return (
    <>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div role="group" aria-label="Durum filtresi" className="flex rounded-md border border-line bg-surface p-0.5">
          {(["all", "active", "passive"] as const).map((value) => (
            <button
              key={value}
              type="button"
              aria-pressed={statusFilter === value}
              onClick={() => setStatusFilter(value)}
              className={[
                "rounded-[9px] px-3 py-1.5 text-[13px] font-medium transition-colors",
                statusFilter === value ? "bg-brand-600 text-white" : "text-ink-2 hover:text-ink",
              ].join(" ")}
            >
              {value === "all" ? "Tümü" : value === "active" ? "Aktif" : "Pasif"}
            </button>
          ))}
        </div>
        <Button size="sm" onClick={() => setCreating(true)}>
          Eğitmen ekle
        </Button>
      </div>
      <AsyncBoundary state={instructors} isEmpty={visibleInstructors.length === 0}>
        {visibleInstructors.length === 0 ? (
          <EmptyState title="Eğitmen yok" description="Seanslara atanacak eğitmenleri ekleyin." />
        ) : (
          <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {visibleInstructors.map((instructor) => (
              <li key={instructor.id} className="flex flex-col gap-2 rounded-lg border border-line bg-surface p-4 shadow-xs">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium text-ink">{instructor.fullName}</p>
                  {instructor.isActive ? <Badge tone="success">Aktif</Badge> : <Badge>Pasif</Badge>}
                </div>
                <p className="text-[13px] text-ink-2">
                  {instructor.phone ?? "—"}
                  {instructor.email !== null ? ` · ${instructor.email}` : ""}
                </p>
                <div className="mt-auto flex gap-2 pt-2">
                  <Button variant="secondary" size="sm" onClick={() => setEditing(instructor)}>
                    Düzenle
                  </Button>
                  {instructor.isActive ? (
                    <Button variant="ghost" size="sm" className="text-danger" onClick={() => setDeactivating(instructor)}>
                      Pasife al
                    </Button>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        )}
      </AsyncBoundary>

      <InstructorDialog key={editing?.id ?? "new"} open={creating || editing !== null} existing={editing} onClose={() => { setCreating(false); setEditing(null); }} />
      <ConfirmDialog
        open={deactivating !== null}
        onClose={() => setDeactivating(null)}
        title="Eğitmen pasife alınsın mı?"
        description={`${deactivating?.fullName ?? ""} artık yeni seans atamalarında sunulmaz.`}
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

function InstructorDialog({
  open,
  existing,
  onClose,
}: {
  readonly open: boolean;
  readonly existing: InstructorDto | null;
  readonly onClose: () => void;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [values, setValues] = useState({
    fullName: existing?.fullName ?? "",
    phone: existing?.phone ?? "",
    email: existing?.email ?? "",
  });

  const save = useMutation({
    mutationFn: () =>
      existing === null
        ? api.company.createInstructor({ fullName: values.fullName.trim(), phone: values.phone.trim() || null, email: values.email.trim() || null })
        : api.company.updateInstructor(existing.id, {
            fullName: values.fullName.trim(),
            phone: values.phone.trim() || null,
            email: values.email.trim() || null,
            isActive: existing.isActive,
          }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.instructors(companyId) });
      toastSuccess(existing === null ? "Eğitmen eklendi." : "Eğitmen güncellendi.");
      onClose();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Kaydedilemedi."),
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={existing === null ? "Eğitmen ekle" : "Eğitmeni düzenle"}
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>Vazgeç</Button>
          <Button size="sm" pending={save.isPending} onClick={() => save.mutate()}>Kaydet</Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <FormField label="Ad Soyad" required>
          {(id) => (
            <Input id={id} value={values.fullName} onChange={(e) => setValues((v) => ({ ...v, fullName: e.target.value }))} />
          )}
        </FormField>
        <FormField label="Telefon">
          {(id) => (
            <Input id={id} type="tel" value={values.phone} onChange={(e) => setValues((v) => ({ ...v, phone: e.target.value }))} />
          )}
        </FormField>
        <FormField label="E-posta">
          {(id) => (
            <Input id={id} type="email" value={values.email} onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))} />
          )}
        </FormField>
      </div>
    </Dialog>
  );
}
