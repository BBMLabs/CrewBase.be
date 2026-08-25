import { useEffect, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { AppError } from "@crewbase/api-client";
import { Button, FormField, Input } from "@crewbase/design-system";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router";
import { useApi } from "../../app/api-context";
import { resolveClubSubdomain, persistClub } from "../../app/club";
import { useSessionStore } from "./session-store";

const loginSchema = z.object({
  email: z.string().min(1).email(),
  password: z.string().min(1),
});

export function MemberLoginPage() {
  const api = useApi();
  const navigate = useNavigate();
  const location = useLocation();
  const [params] = useSearchParams();
  const [subdomain, setSubdomain] = useState<string | null>(() => resolveClubSubdomain());

  const setClub = useSessionStore((s) => s.setClub);
  const applySession = useSessionStore((s) => s.applySession);

  function confirmClub(candidate: string): void {
    persistClub(candidate);
    setClub(candidate, "");
    setSubdomain(candidate);
  }

  const clubInfo = useQuery({
    queryKey: ["public", subdomain ?? "", "info"],
    queryFn: () => api.public.clubInfo(subdomain ?? ""),
    enabled: subdomain !== null && subdomain !== "",
    retry: false,
    staleTime: 5 * 60_000,
  });

  useEffect(() => {
    if (clubInfo.data !== undefined) {
      setClub(subdomain ?? "", clubInfo.data.name);
    }
  }, [clubInfo.data, setClub, subdomain]);

  const form = useForm<z.infer<typeof loginSchema>>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const expired = params.get("oturum") === "sona-erdi";

  const login = useMutation({
    mutationFn: (values: z.infer<typeof loginSchema>) => api.session.login(subdomain ?? "", values),
    onSuccess: (res) => {
      setClub(subdomain ?? "", clubInfo.data?.name ?? "");
      applySession({ member: res.member, accessToken: res.accessToken, expiresAtUtc: res.expiresAtUtc });
      const from = (location.state as { from?: string } | null)?.from ?? "/";
      void navigate(from, { replace: true });
    },
  });

  if (subdomain === null || subdomain === "") {
    return (
      <AuthFrame title="Kulübünüze ulaşın">
        <p className="text-sm leading-relaxed text-ink-2">
          Üretimde kulüpler <code className="font-mono text-[13px]">*.faturebase.com</code> adresinden
          yayınlanır. Yerel geliştirmede kulüp alt alan adını buraya girin — seçiminiz hatırlanır.
        </p>
        <form
          className="mt-4 flex items-end gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            const candidate = new FormData(e.currentTarget).get("club");
            if (typeof candidate === "string" && candidate.trim() !== "") confirmClub(candidate.trim().toLowerCase());
          }}
          noValidate
        >
          <FormField label="Kulüp alt alan adı" required className="flex-1">
            {(id) => (
              <Input id={id} name="club" placeholder="kulupadi" autoComplete="off" />
            )}
          </FormField>
          <Button type="submit">Git</Button>
        </form>
      </AuthFrame>
    );
  }

  if (clubInfo.isError) {
    return (
      <AuthFrame title="Kulüp bulunamadı">
        <p role="alert" className="text-sm text-danger">
          Bu adrese kayıtlı bir kulüp yok. Adresi kontrol edin.
        </p>
      </AuthFrame>
    );
  }

  return (
    <AuthFrame
      title={clubInfo.data !== undefined ? `${clubInfo.data.name} — Üye Girişi` : "Üye Girişi"}
      subtitle={expired ? "Oturumunuzun süresi doldu; lütfen yeniden giriş yapın." : undefined}
    >
      <form className="flex flex-col gap-4" noValidate onSubmit={form.handleSubmit((v) => login.mutate(v))}>
        <FormField label="E-posta" required error={form.formState.errors.email?.message}>
          {(id) => (
            <Input id={id} type="email" autoComplete="email" {...form.register("email")} invalid={form.formState.errors.email !== undefined} />
          )}
        </FormField>
        <FormField label="Parola" required error={form.formState.errors.password?.message}>
          {(id) => (
            <Input id={id} type="password" autoComplete="current-password" {...form.register("password")} invalid={form.formState.errors.password !== undefined} />
          )}
        </FormField>
        {login.isError ? <FormError error={login.error} fallback="Giriş yapılamadı." /> : null}
        <Button type="submit" pending={login.isPending} className="w-full">Giriş yap</Button>
        <p className="text-center text-[13px] text-ink-3">
          Hesabınız yok mu?{" "}
          <Link to="/kayit" className="text-brand-600 hover:underline">Üye olun</Link>
        </p>
      </form>
    </AuthFrame>
  );
}

const registerSchema = z.object({
  fullName: z.string().trim().min(1, "Ad zorunludur.").max(200),
  phone: z.string().trim().min(1, "Telefon zorunludur.").max(20),
  email: z.string().min(1).email().max(254),
  password: z.string().min(8, "Parola en az 8 karakter olmalıdır.").max(128),
});

export function MemberRegisterPage() {
  const api = useApi();
  const navigate = useNavigate();
  const subdomain = resolveClubSubdomain();
  const applySession = useSessionStore((s) => s.applySession);
  const setClub = useSessionStore((s) => s.setClub);

  const clubInfo = useQuery({
    queryKey: ["public", subdomain ?? "", "info"],
    queryFn: () => api.public.clubInfo(subdomain ?? ""),
    enabled: subdomain !== null && subdomain !== "",
    retry: false,
    staleTime: 5 * 60_000,
  });

  const consents = useQuery({
    queryKey: ["public", subdomain ?? "", "consents"],
    queryFn: () => api.public.consents(subdomain ?? ""),
    enabled: subdomain !== null && subdomain !== "",
    staleTime: 10 * 60_000,
  });

  const [accepted, setAccepted] = useState<string[]>([]);
  const memberConsents = (consents.data ?? []).filter((c) => c.scope === "Member");
  const requiredConsents = memberConsents.filter((c) => c.required);

  const form = useForm<z.infer<typeof registerSchema>>({
    resolver: zodResolver(registerSchema),
    defaultValues: { fullName: "", phone: "", email: "", password: "" },
  });

  const register = useMutation({
    mutationFn: (values: z.infer<typeof registerSchema>) =>
      api.session.register(subdomain ?? "", {
        fullName: values.fullName.trim(),
        phone: values.phone.trim(),
        email: values.email.trim(),
        password: values.password,
        acceptedConsents: accepted,
      }),
    onSuccess: (res) => {
      setClub(subdomain ?? "", clubInfo.data?.name ?? "");
      applySession({ member: res.member, accessToken: res.accessToken, expiresAtUtc: res.expiresAtUtc });
      void navigate("/", { replace: true });
    },
  });

  if (subdomain === null || subdomain === "") {
    return (
      <AuthFrame>
        <p role="alert" className="text-sm text-danger">Kulüp adresi bulunamadı.</p>
      </AuthFrame>
    );
  }

  return (
    <AuthFrame title={clubInfo.data !== undefined ? `${clubInfo.data.name} — Üyelik` : "Üyelik Oluştur"}>
      <form
        className="flex flex-col gap-4"
        noValidate
        onSubmit={form.handleSubmit(
          (values) => {
            const missingRequired = requiredConsents.filter((c) => !accepted.includes(c.key));
            if (missingRequired.length > 0) {
              form.setError("root", {
                message: `Devam etmek için şu beyanları onaylamalısınız: ${missingRequired.map((c) => c.title).join(", ")}`,
              });
              return;
            }
            form.clearErrors("root");
            register.mutate(values);
          },
        )}
      >
        <FormField label="Ad Soyad" required error={form.formState.errors.fullName?.message}>
          {(id) => <Input id={id} {...form.register("fullName")} invalid={form.formState.errors.fullName !== undefined} />}
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="Telefon" required hint="Daha önce misafir randevusu aldıysanız aynı telefonla hesabınıza kavuşursunuz." error={form.formState.errors.phone?.message}>
            {(id) => <Input id={id} type="tel" {...form.register("phone")} invalid={form.formState.errors.phone !== undefined} />}
          </FormField>
          <FormField label="E-posta" required error={form.formState.errors.email?.message}>
            {(id) => <Input id={id} type="email" {...form.register("email")} invalid={form.formState.errors.email !== undefined} />}
          </FormField>
        </div>
        <FormField label="Parola" required hint="En az 8 karakter." error={form.formState.errors.password?.message}>
          {(id) => <Input id={id} type="password" autoComplete="new-password" {...form.register("password")} invalid={form.formState.errors.password !== undefined} />}
        </FormField>

        <fieldset className="flex flex-col gap-3 rounded-md border border-line p-3">
          <legend className="px-1 text-[13px] font-medium text-ink-2">Beyanlar</legend>
          {memberConsents.map((consent) => {
            const checked = accepted.includes(consent.key);
            return (
              <label key={consent.key} className="flex items-start gap-2.5">
                <input
                  type="checkbox"
                  className="mt-0.5 size-4 accent-brand-600"
                  checked={checked}
                  onChange={(e) =>
                    setAccepted((prev) => (e.target.checked ? [...prev, consent.key] : prev.filter((k) => k !== consent.key)))
                  }
                />
                <span>
                  <span className="text-[13px] font-semibold text-ink">
                    {consent.icon} {consent.title}
                    {consent.required ? <span className="ml-1 text-danger">*</span> : null}
                  </span>
                  <span className="block text-xs leading-5 text-ink-3">{consent.body}</span>
                </span>
              </label>
            );
          })}
          {requiredConsents.length > 0 ? (
            <p className="rounded-md bg-warning-bg px-2.5 py-1.5 text-xs text-warning">
              <span className="text-danger">*</span> işaretli beyanlar üyelik için zorunludur.
            </p>
          ) : null}
        </fieldset>

        {form.formState.errors.root?.message !== undefined ? (
          <p role="alert" className="text-sm font-medium text-danger">{form.formState.errors.root.message}</p>
        ) : null}
        {register.isError ? <FormError error={register.error} fallback="Üyelik oluşturulamadı." /> : null}
        <Button type="submit" pending={register.isPending} className="w-full">Üye ol</Button>
        <p className="text-center text-[13px] text-ink-3">
          Hesabınız var mı?{" "}
          <Link to="/giris" className="text-brand-600 hover:underline">Giriş yapın</Link>
        </p>
      </form>
    </AuthFrame>
  );
}

function FormError({ error, fallback }: { readonly error: unknown; readonly fallback: string }) {
  if (!(error instanceof AppError)) {
    return <p role="alert" className="text-sm font-medium text-danger">{fallback}</p>;
  }
  return (
    <p role="alert" className="text-sm font-medium text-danger">
      {error.serverMessage ?? fallback}
    </p>
  );
}

function AuthFrame({
  title,
  subtitle,
  children,
}: {
  readonly title?: string;
  readonly subtitle?: string;
  readonly children: React.ReactNode;
}) {
  return (
    <div className="flex min-h-dvh flex-col">
      {/* Compact branded top band with calm wave accent */}
      <div className="relative overflow-hidden bg-rail pb-12 pt-8">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{ background: "radial-gradient(40rem 20rem at 110% -20%, rgb(64 144 196 / 0.35) 0%, transparent 60%)" }}
        />
        <div className="relative mx-auto max-w-lg px-4">
          <div className="cb-rise flex items-center gap-2.5">
            <span
              aria-hidden="true"
              className="flex size-8 items-center justify-center rounded-lg bg-linear-to-br from-brand-400 to-brand-600 shadow-xs"
            >
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path
                  d="M3 16c2.2 0 2.2-1.8 4.5-1.8S9.7 16 12 16s2.3-1.8 4.5-1.8S19.8 16 21 16M6 11l6-7 6 7"
                  stroke="#fff"
                  strokeWidth="1.9"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
            <span className="font-display text-[15px] font-extrabold tracking-[-0.01em] text-white">CrewBase</span>
            <span className="ml-auto text-[10.5px] font-semibold uppercase tracking-[0.14em] text-rail-text">Üye portalı</span>
          </div>
          <p className="cb-rise mt-2.5 font-display text-lg font-bold leading-snug text-white/95" style={{ animationDelay: "80ms" }}>
            Birlikte kürek çekmenin dijital merkezi.
          </p>
        </div>
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-0 bottom-0 flex w-[200%] text-brand-300/30 wave-drift"
          style={{ ["--wave-duration" as string]: "20s" }}
        >
          <svg className="h-9 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none">
            <path d="M0 18 C 90 4, 180 30, 300 18 S 480 4, 600 18 L 600 40 L 0 40 Z" fill="currentColor" />
          </svg>
          <svg className="h-9 w-1/2 shrink-0" viewBox="0 0 600 40" preserveAspectRatio="none" fill="none">
            <path d="M0 18 C 90 4, 180 30, 300 18 S 480 4, 600 18 L 600 40 L 0 40 Z" fill="currentColor" />
          </svg>
        </div>
      </div>

      {/* Form overlaps the band slightly for depth */}
      <main className="mx-auto -mt-7 w-full max-w-lg px-4 pb-12">
        <section className="cb-rise rounded-xl border border-line/70 bg-surface p-6 shadow-md sm:p-7" style={{ animationDelay: "120ms" }}>
          {title !== undefined ? (
            <h1 className="font-display text-xl font-extrabold tracking-[-0.02em] text-ink">{title}</h1>
          ) : null}
          {subtitle !== undefined ? <p className="mt-1.5 text-sm font-medium text-warning">{subtitle}</p> : null}
          <div className={title !== undefined ? "mt-5" : ""}>{children}</div>
        </section>
      </main>
    </div>
  );
}
