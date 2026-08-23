import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CompanyUserDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  Button,
  Dialog,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  USER_ROLE_LABELS,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { formatIsoDateTr } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";

export function UsersPage() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const sessionEmail = useSessionStore((s) => s.email);
  const [creating, setCreating] = useState(false);

  const users = useQuery({ queryKey: panelKeys.users(companyId), queryFn: () => api.company.users(), retry: false });

  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        title="Kullanıcılar"
        description="Firmanın panel kullanıcıları. Yetki denetimi her istekte sunucuda yeniden doğrulanır."
        actions={
          <Button size="sm" onClick={() => setCreating(true)}>
            Kullanıcı ekle
          </Button>
        }
      />

      <AsyncBoundary state={users} isEmpty={(users.data?.length ?? 0) === 0}>
        {(users.data?.length ?? 0) === 0 ? (
          <EmptyState title="Kullanıcı yok" />
        ) : (
          <div className="overflow-x-auto rounded-lg border border-line bg-surface shadow-xs">
            <table className="w-full min-w-[560px] text-sm">
              <caption className="sr-only">Panel kullanıcıları</caption>
              <thead>
                <tr className="border-b border-line bg-surface-2 text-left text-xs uppercase tracking-wide text-ink-3">
                  <th scope="col" className="px-4 py-2.5">E-posta</th>
                  <th scope="col" className="px-4 py-2.5">Rol</th>
                  <th scope="col" className="px-4 py-2.5">Durum</th>
                  <th scope="col" className="px-4 py-2.5">Oluşturulma</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {(users.data ?? []).map((user) => (
                  <tr key={user.id}>
                    <td className="px-4 py-2.5 font-medium text-ink">
                      {user.email}
                      {user.email === sessionEmail ? <Badge tone="info" dot={false} className="ml-2">(sen)</Badge> : null}
                    </td>
                    <td className="px-4 py-2.5"><RoleCell user={user} isSelf={user.email === sessionEmail} /></td>
                    <td className="px-4 py-2.5">
                      {user.status === "Active" ? <Badge tone="success">Aktif</Badge> : <Badge tone="danger">Devre dışı</Badge>}
                    </td>
                    <td className="px-4 py-2.5 text-ink-3">{formatIsoDateTr(user.createdAtUtc.slice(0, 10))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </AsyncBoundary>

      <CreateUserDialog open={creating} onClose={() => setCreating(false)} />
    </div>
  );
}

function RoleCell({ user, isSelf }: { readonly user: CompanyUserDto; readonly isSelf: boolean }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();

  const changeRole = useMutation({
    mutationFn: (role: "CompanyAdmin" | "Employee") => api.company.changeUserRole(user.id, role),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.users(companyId) });
      toastSuccess("Kullanıcı rolü güncellendi.");
    },
    onError: async (error) => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.users(companyId) });
      toastError(error instanceof Error ? error.message : "Rol değiştirilemedi.");
    },
  });

  return (
    <label className="flex items-center gap-2">
      <span className="sr-only">{user.email} için rol seç</span>
      <select
        value={user.role}
        disabled={changeRole.isPending || isSelf}
        title={isSelf ? "Kendi rolünüz panel tarafından değiştirilemez" : undefined}
        onChange={(e) => changeRole.mutate(e.target.value as "CompanyAdmin" | "Employee")}
        className="h-8 rounded-sm border border-line bg-surface px-2 text-xs"
      >
        <option value="CompanyAdmin">{USER_ROLE_LABELS["CompanyAdmin"]}</option>
        <option value="Employee">{USER_ROLE_LABELS["Employee"]}</option>
      </select>
    </label>
  );
}

function CreateUserDialog({ open, onClose }: { readonly open: boolean; readonly onClose: () => void }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [values, setValues] = useState({ email: "", password: "", role: "Employee" });

  const create = useMutation({
    mutationFn: () =>
      api.company.createUser({
        email: values.email.trim(),
        password: values.password,
        role: values.role as "CompanyAdmin" | "Employee",
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.users(companyId) });
      toastSuccess("Kullanıcı oluşturuldu.");
      setValues({ email: "", password: "", role: "Employee" });
      onClose();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Kullanıcı oluşturulamadı."),
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Kullanıcı ekle"
      description="PlatformAdmin rolü verilemez; yalnızca kendi firmanız içinde yetki dağıtabilirsiniz."
      footer={
        <>
          <Button variant="secondary" size="sm" onClick={onClose}>Vazgeç</Button>
          <Button size="sm" pending={create.isPending} onClick={() => create.mutate()}>Oluştur</Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <FormField label="E-posta" required>
          {(id) => (
            <Input id={id} type="email" value={values.email} onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))} />
          )}
        </FormField>
        <FormField label="Parola" required hint="Kullanıcı ilk girişinde bu parolayı kullanır.">
          {(id) => (
            <Input id={id} type="password" autoComplete="new-password" value={values.password} onChange={(e) => setValues((v) => ({ ...v, password: e.target.value }))} />
          )}
        </FormField>
        <FormField label="Rol" required>
          {(id) => (
            <select
              id={id}
              value={values.role}
              onChange={(e) => setValues((v) => ({ ...v, role: e.target.value }))}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm"
            >
              <option value="Employee">{USER_ROLE_LABELS["Employee"]}</option>
              <option value="CompanyAdmin">{USER_ROLE_LABELS["CompanyAdmin"]}</option>
            </select>
          )}
        </FormField>
      </div>
    </Dialog>
  );
}
