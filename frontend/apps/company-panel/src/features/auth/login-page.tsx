import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import type { LoginResult } from "@crewbase/api-types";
import { AppError } from "@crewbase/api-client";
import { Button, FormField, Input } from "@crewbase/design-system";
import { Link, useLocation, useNavigate } from "react-router";
import { useApi } from "../../app/api-context";
import { useSessionStore } from "./session-store";
import { AuthCard, AuthLayout } from "./auth-layout";

const loginSchema = z.object({
  email: z.string().min(1, "E-posta zorunludur.").email("Geçerli bir e-posta girin."),
  password: z.string().min(1, "Parola zorunludur."),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const api = useApi();
  const navigate = useNavigate();
  const location = useLocation();
  const applySnapshot = useSessionStore((s) => s.applySnapshot);

  const form = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
    mode: "onBlur",
  });

  const mutation = useMutation({
    mutationFn: (values: LoginForm): Promise<LoginResult> =>
      api.session.login(values.email, values.password),
    onSuccess: async (result) => {
      if (result.requiresTwoFactor === true) {
        // Backend keeps 2FA routes disabled (404) — defensive branch only.
        form.setError("root", { message: "İki adımlı doğrulama şu anda kapalıdır." });
        return;
      }
      applySnapshot(api.session.snapshot());
      const from = (location.state as { from?: string } | null)?.from ?? "/";
      void navigate(from, { replace: true });
    },
    onError: (error) => {
      if (!(error instanceof AppError)) return;
      if (error.fieldErrors !== undefined) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          if (field === "email" || field === "password") {
            form.setError(field, { message: messages[0] });
          }
        }
      }
      form.setError("root", { message: error.serverMessage ?? "Giriş yapılamadı." });
    },
  });

  return (
    <AuthLayout>
      <AuthCard title="Panele giriş yap" subtitle="Kulübünüzü yönetmek için hesabınıza erişin.">
        <form
          className="flex flex-col gap-4"
          onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
          noValidate
        >
          <FormField label="E-posta" required error={form.formState.errors.email?.message}>
            {(id) => (
              <Input
                id={id}
                type="email"
                autoComplete="email"
                placeholder="ad@kulup.com"
                invalid={form.formState.errors.email !== undefined}
                {...form.register("email")}
              />
            )}
          </FormField>
          <FormField label="Parola" required error={form.formState.errors.password?.message}>
            {(id) => (
              <Input
                id={id}
                type="password"
                autoComplete="current-password"
                placeholder="••••••••••"
                invalid={form.formState.errors.password !== undefined}
                {...form.register("password")}
              />
            )}
          </FormField>
          {form.formState.errors.root?.message !== undefined ? (
            <p role="alert" className="rounded-md bg-danger-bg px-3 py-2 text-sm font-medium text-danger">
              {form.formState.errors.root.message}
            </p>
          ) : null}
          <Button type="submit" pending={mutation.isPending} className="mt-1 w-full">
            Giriş yap
          </Button>
        </form>
        <div className="mt-6 border-t border-line/70 pt-4 text-center text-[13px]">
          <span className="text-ink-3">Parolamı unuttum → </span>
          <Link to="/sifremi-unuttum" className="font-semibold text-brand-600 hover:text-brand-700 hover:underline">
            sıfırla
          </Link>
          <span className="mx-2 text-line-strong">·</span>
          <Link to="/kayit" className="font-semibold text-brand-600 hover:text-brand-700 hover:underline">
            Kulüp kaydı oluştur
          </Link>
        </div>
      </AuthCard>
    </AuthLayout>
  );
}
