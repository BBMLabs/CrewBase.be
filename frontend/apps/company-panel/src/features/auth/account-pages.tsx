import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { AppError } from "@crewbase/api-client";
import { Button, FormField, Input } from "@crewbase/design-system";
import { Link, useNavigate, useSearchParams } from "react-router";
import { useApi } from "../../app/api-context";
import { toastSuccess } from "../../shared/toast-store";
import { AuthCard, AuthLayout } from "./auth-layout";

/** Mirrors RegisterCompanyCommandValidator (verified source). */
const signupSchema = z.object({
  companyName: z.string().min(1, "Kulüp adı zorunludur.").max(200),
  adminEmail: z.string().min(1).email("Geçerli bir e-posta girin.").max(254),
  adminPassword: z
    .string()
    .min(10, "Parola en az 10 karakter olmalıdır.")
    .max(128)
    .regex(/[A-Z]/, "En az bir büyük harf içermelidir.")
    .regex(/[a-z]/, "En az bir küçük harf içermelidir.")
    .regex(/[0-9]/, "En az bir rakam içermelidir.")
    .regex(/[^a-zA-Z0-9]/, "En az bir özel karakter içermelidir."),
  phone: z
    .string()
    .regex(/^\+?[0-9\s\-()]{7,20}$/, "Geçerli bir telefon girin.")
    .optional()
    .or(z.literal("")),
  contactEmail: z.string().email("Geçerli bir e-posta girin.").optional().or(z.literal("")),
  address: z.string().max(500).optional().or(z.literal("")),
});

type SignupForm = z.infer<typeof signupSchema>;

export function SignupPage() {
  const api = useApi();
  const form = useForm<SignupForm>({
    resolver: zodResolver(signupSchema),
    defaultValues: { companyName: "", adminEmail: "", adminPassword: "", phone: "", contactEmail: "", address: "" },
  });
  const created = useMutation({
    mutationFn: (values: SignupForm) =>
      api.auth.registerCompany({
        companyName: values.companyName,
        adminEmail: values.adminEmail,
        adminPassword: values.adminPassword,
        phone: values.phone === "" ? null : values.phone,
        contactEmail: values.contactEmail === "" ? null : values.contactEmail,
        address: values.address === "" ? null : values.address,
      }),
  });

  if (created.isSuccess) {
    const data = created.data;
    return (
      <AuthShell title="Kulübünüz hazır">
        <div className="flex flex-col gap-3 text-sm text-ink-2">
          <p className="text-ink">
            <strong>{data.companyName}</strong> kulübünüz doğrudan aktif olarak oluşturuldu.
          </p>
          <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 rounded-md bg-surface-2 p-3 text-[13px]">
            <dt className="text-ink-3">Alt alan adı</dt>
            <dd className="font-mono">{data.subdomain}</dd>
            <dt className="text-ink-3">Site adresi</dt>
            <dd className="break-all font-mono">{data.siteUrl}</dd>
            <dt className="text-ink-3">Yönetici e-postası</dt>
            <dd>{data.adminEmail}</dd>
          </dl>
          <p>Hoş geldiniz e-postası gönderildi. Parolanız asla e-posta ile paylaşılmaz.</p>
          <Link to="/giris" className="mt-2 text-center text-brand-600 hover:underline">
            Panele giriş yap →
          </Link>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell title="Kulüp kaydı oluştur" subtitle="Tek adımda kulübünüzü ve yönetici hesabınızı açın.">
      <form
        className="flex flex-col gap-4"
        noValidate
        onSubmit={form.handleSubmit((values) => created.mutate(values))}
      >
        <FormField label="Kulüp adı" required error={form.formState.errors.companyName?.message}>
          {(id) => (
            <Input id={id} {...form.register("companyName")} invalid={form.formState.errors.companyName !== undefined} />
          )}
        </FormField>
        <FormField label="Yönetici e-postası" required error={form.formState.errors.adminEmail?.message}>
          {(id) => (
            <Input
              id={id}
              type="email"
              autoComplete="email"
              {...form.register("adminEmail")}
              invalid={form.formState.errors.adminEmail !== undefined}
            />
          )}
        </FormField>
        <FormField
          label="Parola"
          required
          hint="En az 10 karakter; büyük/küçük harf, rakam ve özel karakter içermelidir."
          error={form.formState.errors.adminPassword?.message}
        >
          {(id) => (
            <Input
              id={id}
              type="password"
              autoComplete="new-password"
              {...form.register("adminPassword")}
              invalid={form.formState.errors.adminPassword !== undefined}
            />
          )}
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="Telefon" error={form.formState.errors.phone?.message}>
            {(id) => <Input id={id} type="tel" {...form.register("phone")} />}
          </FormField>
          <FormField label="İletişim e-postası" error={form.formState.errors.contactEmail?.message}>
            {(id) => (
              <Input id={id} type="email" {...form.register("contactEmail")} invalid={form.formState.errors.contactEmail !== undefined} />
            )}
          </FormField>
        </div>
        <FormField label="Adres" error={form.formState.errors.address?.message}>
          {(id) => <Input id={id} {...form.register("address")} />}
        </FormField>
        {created.isError ? <RootError error={created.error} fallback="Kayıt tamamlanamadı." /> : null}
        <Button type="submit" pending={created.isPending} className="w-full">
          Kulübümü oluştur
        </Button>
        <p className="text-center text-[13px] text-ink-3">
          Zaten hesabınız var mı?{" "}
          <Link to="/giris" className="text-brand-600 hover:underline">
            Giriş yapın
          </Link>
        </p>
      </form>
    </AuthShell>
  );
}

const forgotSchema = z.object({ email: z.string().min(1).email() });
type ForgotForm = z.infer<typeof forgotSchema>;

export function ForgotPasswordPage() {
  const api = useApi();
  const form = useForm<ForgotForm>({ resolver: zodResolver(forgotSchema), defaultValues: { email: "" } });
  const mutation = useMutation({
    mutationFn: (values: ForgotForm) => api.auth.forgotPassword(values.email),
  });

  return (
    <AuthShell title="Parolamı unuttum" subtitle="Kayıtlı e-posta adresinizi girin; sıfırlama bağlantısı gönderilecek.">
      {mutation.isSuccess ? (
        <p role="status" className="rounded-md bg-success-bg px-3 py-2 text-sm text-success">
          Parola sıfırlama bağlantısı e-posta adresinize gönderildi.
        </p>
      ) : (
        <form className="flex flex-col gap-4" noValidate onSubmit={form.handleSubmit((v) => mutation.mutate(v))}>
          <FormField label="E-posta" required error={form.formState.errors.email?.message}>
            {(id) => (
              <Input id={id} type="email" {...form.register("email")} invalid={form.formState.errors.email !== undefined} />
            )}
          </FormField>
          {mutation.isError ? <RootError error={mutation.error} fallback="İstek gönderilemedi." /> : null}
          <Button type="submit" pending={mutation.isPending} className="w-full">
            Bağlantı gönder
          </Button>
        </form>
      )}
      <p className="mt-5 text-center text-[13px]">
        <Link to="/giris" className="text-brand-600 hover:underline">
          Girişe dön
        </Link>
      </p>
    </AuthShell>
  );
}

const resetSchema = z.object({
  email: z.string().min(1).email(),
  token: z.string().min(1, "Sıfırlama kodu zorunludur."),
  newPassword: z
    .string()
    .min(10, "Parola en az 10 karakter olmalıdır.")
    .regex(/[A-Z]/, "En az bir büyük harf içermelidir.")
    .regex(/[a-z]/, "En az bir küçük harf içermelidir.")
    .regex(/[0-9]/, "En az bir rakam içermelidir.")
    .regex(/[^a-zA-Z0-9]/, "En az bir özel karakter içermelidir."),
});
type ResetForm = z.infer<typeof resetSchema>;

export function ResetPasswordPage() {
  const api = useApi();
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const form = useForm<ResetForm>({
    resolver: zodResolver(resetSchema),
    defaultValues: { email: params.get("email") ?? "", token: params.get("token") ?? "", newPassword: "" },
  });
  const mutation = useMutation({
    mutationFn: (values: ResetForm) =>
      api.auth.resetPassword({ email: values.email, token: values.token, newPassword: values.newPassword }),
    onSuccess: () => {
      toastSuccess("Parolanız güncellendi. Giriş yapabilirsiniz.");
      void navigate("/giris");
    },
  });

  return (
    <AuthShell title="Yeni parola belirle">
      <form className="flex flex-col gap-4" noValidate onSubmit={form.handleSubmit((v) => mutation.mutate(v))}>
        <FormField label="E-posta" required error={form.formState.errors.email?.message}>
          {(id) => <Input id={id} type="email" {...form.register("email")} invalid={form.formState.errors.email !== undefined} />}
        </FormField>
        <FormField label="Sıfırlama kodu" required error={form.formState.errors.token?.message}>
          {(id) => <Input id={id} {...form.register("token")} invalid={form.formState.errors.token !== undefined} />}
        </FormField>
        <FormField
          label="Yeni parola"
          required
          hint="En az 10 karakter; büyük/küçük harf, rakam ve özel karakter."
          error={form.formState.errors.newPassword?.message}
        >
          {(id) => (
            <Input
              id={id}
              type="password"
              autoComplete="new-password"
              {...form.register("newPassword")}
              invalid={form.formState.errors.newPassword !== undefined}
            />
          )}
        </FormField>
        {mutation.isError ? <RootError error={mutation.error} fallback="Parola güncellenemedi." /> : null}
        <Button type="submit" pending={mutation.isPending} className="w-full">
          Parolayı güncelle
        </Button>
      </form>
    </AuthShell>
  );
}

const verifySchema = z.object({
  email: z.string().min(1).email(),
  code: z.string().regex(/^\d{6}$/, "6 haneli kodu girin."),
});
type VerifyForm = z.infer<typeof verifySchema>;

export function VerifyEmailPage() {
  const api = useApi();
  const [params] = useSearchParams();
  const form = useForm<VerifyForm>({
    resolver: zodResolver(verifySchema),
    defaultValues: { email: params.get("email") ?? "", code: "" },
  });

  const sendCode = useMutation({ mutationFn: (email: string) => api.auth.sendVerificationEmail(email) });
  const verify = useMutation({
    mutationFn: (values: VerifyForm) => api.auth.verifyEmail({ email: values.email, token: values.code }),
    onSuccess: () => toastSuccess("E-posta adresiniz doğrulandı."),
  });

  return (
    <AuthShell title="E-postanı doğrula" subtitle="E-postana gönderilen 6 haneli kodu gir.">
      <form className="flex flex-col gap-4" noValidate onSubmit={form.handleSubmit((v) => verify.mutate(v))}>
        <FormField label="E-posta" required error={form.formState.errors.email?.message}>
          {(id) => <Input id={id} type="email" {...form.register("email")} invalid={form.formState.errors.email !== undefined} />}
        </FormField>
        <FormField label="Doğrulama kodu" required error={form.formState.errors.code?.message}>
          {(id) => (
            <Input
              id={id}
              inputMode="numeric"
              maxLength={6}
              placeholder="••••••"
              className="text-center font-mono tracking-[0.5em]"
              {...form.register("code")}
              invalid={form.formState.errors.code !== undefined}
            />
          )}
        </FormField>
        {verify.isError ? <RootError error={verify.error} fallback="Doğrulama başarısız." /> : null}
        <Button type="submit" pending={verify.isPending} className="w-full">
          Doğrula
        </Button>
        <Button
          variant="ghost"
          size="sm"
          pending={sendCode.isPending}
          onClick={() => {
            const email = form.getValues("email");
            if (email !== "") void sendCode.mutate(email);
          }}
        >
          Kodu tekrar gönder
        </Button>
      </form>
    </AuthShell>
  );
}

function RootError({ error, fallback }: { readonly error: unknown; readonly fallback: string }) {
  if (!(error instanceof AppError)) {
    return (
      <p role="alert" className="text-sm font-medium text-danger">
        {fallback}
      </p>
    );
  }
  return (
    <>
      <p role="alert" className="text-sm font-medium text-danger">
        {error.serverMessage ?? fallback}
      </p>
      {error.fieldErrors !== undefined
        ? Object.entries(error.fieldErrors).map(([field, messages]) =>
            field === "detail" || messages.length === 0 ? null : (
              <p key={`${field}-${messages[0] ?? ""}`} role="alert" className="text-xs text-danger">
                {messages[0]}
              </p>
            ),
          )
        : null}
    </>
  );
}

function AuthShell({
  title,
  subtitle,
  children,
}: {
  readonly title: string;
  readonly subtitle?: string;
  readonly children: React.ReactNode;
}) {
  return (
    <AuthLayout>
      <AuthCard title={title} subtitle={subtitle}>
        {children}
      </AuthCard>
    </AuthLayout>
  );
}
