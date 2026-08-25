import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CardDto } from "@crewbase/api-types";
import {
  Badge,
  Button,
  CARD_STATUS_LABELS,
  Dialog,
  FormField,
  Input,
  PageHeader,
  rowingLevelLabel,
  SectionCard,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { toastError, toastSuccess } from "../../shared/toast-store";

export function ProfilePage() {
  const api = useApi();
  const queryClient = useQueryClient();

  const profile = useQuery({ queryKey: ["member", "me"], queryFn: () => api.member.profile() });
  const invalidateProfile = async () => {
    await queryClient.invalidateQueries({ queryKey: ["member", "me"] });
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Profilim" description={profile.data !== undefined ? rowingLevelLabel(profile.data.level) : undefined} />

      <SectionCard title="Bilgilerim">
        {profile.data !== undefined ? (
          <ProfileForm
            initial={{
              fullName: profile.data.fullName,
              email: profile.data.email ?? "",
              defaultReminderMinutes: profile.data.defaultReminderMinutes,
            }}
            onSaved={invalidateProfile}
          />
        ) : (
          <p className="text-sm text-ink-3">Yükleniyor…</p>
        )}
      </SectionCard>

      <OtpSection onVerified={invalidateProfile} />
      <ConsentsSection />
      <CardsSection />
      <DangerZone />
    </div>
  );
}

function ProfileForm({
  initial,
  onSaved,
}: {
  readonly initial: { fullName: string; email: string; defaultReminderMinutes: number | null };
  readonly onSaved: () => Promise<void>;
}) {
  const api = useApi();
  const [fullName, setFullName] = useState(initial.fullName);
  const [email, setEmail] = useState(initial.email);
  const [reminder, setReminder] = useState(String(initial.defaultReminderMinutes ?? 0));

  const save = useMutation({
    mutationFn: () =>
      api.member.updateProfile({
        fullName: fullName.trim(),
        email: email.trim() === "" ? null : email.trim(),
        defaultReminderMinutes: Number(reminder),
      }),
    onSuccess: async () => {
      toastSuccess("Bilgileriniz güncellendi.");
      await onSaved();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Kaydedilemedi."),
  });

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault();
        save.mutate();
      }}
      noValidate
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label="Ad Soyad" required>
          {(id) => (
            <Input id={id} value={fullName} onChange={(e) => setFullName(e.target.value)} />
          )}
        </FormField>
        <FormField label="E-posta">
          {(id) => (
            <Input id={id} type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          )}
        </FormField>
      </div>
      <FormField label="Varsayılan hatırlatma" hint="Randevu formunda ön-seçim olarak kullanılır. 0 = istemiyorum.">
        {(id) => (
          <select id={id} value={reminder} onChange={(e) => setReminder(e.target.value)} className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm sm:w-56">
            <option value="0">Hatırlatma yok</option>
            <option value="30">30 dk önce</option>
            <option value="60">1 saat önce</option>
            <option value="120">2 saat önce</option>
            <option value="1440">1 gün önce</option>
          </select>
        )}
      </FormField>
      <div>
        <Button type="submit" size="sm" pending={save.isPending}>Kaydet</Button>
      </div>
    </form>
  );
}

function OtpSection({ onVerified }: { readonly onVerified: () => Promise<void> }) {
  const api = useApi();
  const profile = useQuery({ queryKey: ["member", "me"], queryFn: () => api.member.profile() });
  const data = profile.data;
  if (data === undefined) return null;

  return (
    <SectionCard title="Doğrulama" description="E-posta ve telefonunuzu doğrulayın. Kodlar e-postanıza gönderilir.">
      <div className="flex flex-col gap-4 sm:flex-row sm:gap-8">
        <OtpChannel
          channel="email"
          label="E-posta"
          verified={data.emailVerified}
          target={data.email ?? "—"}
          onVerified={onVerified}
        />
        <OtpChannel
          channel="phone"
          label="Telefon"
          verified={data.phoneVerified}
          target={data.phone}
          onVerified={onVerified}
        />
      </div>
    </SectionCard>
  );
}

function OtpChannel({
  channel,
  label,
  verified,
  target,
  onVerified,
}: {
  readonly channel: "email" | "phone";
  readonly label: string;
  readonly verified: boolean;
  readonly target: string;
  readonly onVerified: () => Promise<void>;
}) {
  const api = useApi();
  const [codeSent, setCodeSent] = useState(false);
  const [code, setCode] = useState("");

  const request = useMutation({
    mutationFn: () => api.member.requestOtp(channel),
    onSuccess: () => {
      setCodeSent(true);
      toastSuccess("Kod gönderildi.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Kod gönderilemedi."),
  });

  const verify = useMutation({
    mutationFn: () => api.member.verifyOtp(channel, code.trim()),
    onSuccess: async () => {
      toastSuccess("Doğrulama tamamlandı.");
      await onVerified();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Doğrulanamadı."),
  });

  return (
    <div className="flex flex-col gap-2 sm:flex-1">
      <div className="flex items-center gap-2">
        <p className="text-sm font-medium text-ink">{label}</p>
        {verified ? <Badge tone="success">Doğrulandı</Badge> : <Badge tone="warning">Doğrulanmadı</Badge>}
      </div>
      <p className="font-mono text-[13px] text-ink-2">{target}</p>
      {!verified ? (
        !codeSent ? (
          <Button variant="secondary" size="sm" pending={request.isPending} onClick={() => request.mutate()}>
            Kod gönder
          </Button>
        ) : (
          <div className="flex items-end gap-2">
            <FormField label="6 haneli kod" required className="w-36">
              {(id) => (
                <Input
                  id={id}
                  inputMode="numeric"
                  maxLength={6}
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  className="text-center font-mono tracking-widest"
                />
              )}
            </FormField>
            <Button size="sm" pending={verify.isPending} onClick={() => verify.mutate()}>
              Doğrula
            </Button>
            <Button variant="ghost" size="sm" onClick={() => setCodeSent(false)}>
              Vazgeç
            </Button>
          </div>
        )
      ) : null}
    </div>
  );
}

function ConsentsSection() {
  const api = useApi();
  const queryClient = useQueryClient();
  const consents = useQuery({ queryKey: ["member", "consents"], queryFn: () => api.member.consents() });
  const [pendingChanges, setPendingChanges] = useState<Record<string, boolean>>({});

  const submit = useMutation({
    mutationFn: () =>
      api.member.submitConsents(
        Object.entries(pendingChanges).map(([key, accepted]) => ({ key, accepted })),
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["member", "consents"] });
      setPendingChanges({});
      toastSuccess("Beyanlarınız kaydedildi.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Beyanlar kaydedilemedi."),
  });

  const rows = consents.data ?? [];
  const dirty = Object.keys(pendingChanges).length > 0;

  return (
    <SectionCard title="Beyanlarım" description="Zorunlu beyanlar reddedilemez; isteğe bağlı rızalarınızı geri çekebilirsiniz.">
      {rows.length === 0 ? (
        <p className="text-sm text-ink-3">Yükleniyor…</p>
      ) : (
        <form
          className="flex flex-col gap-3"
          onSubmit={(e) => {
            e.preventDefault();
            submit.mutate();
          }}
        >
          {rows.map((consent) => {
            const effective = pendingChanges[consent.key] ?? consent.accepted;
            return (
              <label key={consent.key} className="flex items-start gap-3 rounded-md border border-line p-3">
                <input
                  type="checkbox"
                  className="mt-1 size-4 accent-brand-600"
                  disabled={consent.required || submit.isPending}
                  checked={effective}
                  onChange={(e) => setPendingChanges((prev) => ({ ...prev, [consent.key]: e.target.checked }))}
                />
                <span className="min-w-0 flex-1">
                  <span className="text-sm font-medium text-ink">
                    {consent.icon} {consent.title}{" "}
                    {consent.required ? <Badge tone="neutral">zorunlu</Badge> : null}
                  </span>
                  <span className="mt-0.5 block text-xs text-ink-3">{consent.body}</span>
                  {consent.accepted && consent.acceptedAtUtc !== null ? (
                    <span className="block text-[11px] text-ink-3">
                      Onay: {consent.acceptedAtUtc.slice(0, 10)}
                    </span>
                  ) : null}
                </span>
              </label>
            );
          })}
          {dirty ? (
            <div className="flex gap-2">
              <Button type="submit" size="sm" pending={submit.isPending}>Kaydet</Button>
              <Button variant="ghost" size="sm" onClick={() => setPendingChanges({})}>Vazgeç</Button>
            </div>
          ) : null}
        </form>
      )}
    </SectionCard>
  );
}

function CardsSection() {
  const api = useApi();
  const queryClient = useQueryClient();
  const cards = useQuery({ queryKey: ["member", "cards"], queryFn: () => api.member.cards() });

  const byType = (type: "Multisport" | "Meditopia"): CardDto | undefined =>
    (cards.data ?? []).find((c) => c.type === type);

  return (
    <SectionCard title="Üyelik Kartları" description="Multisport veya Meditopia kart bilgilerinizi kendiniz yönetirsiniz.">
      <div className="grid gap-4 md:grid-cols-2">
        <CardForm type="Multisport" existing={byType("Multisport")} onSaved={async () => { await queryClient.invalidateQueries({ queryKey: ["member", "cards"] }); }} />
        <CardForm type="Meditopia" existing={byType("Meditopia")} onSaved={async () => { await queryClient.invalidateQueries({ queryKey: ["member", "cards"] }); }} />
      </div>
    </SectionCard>
  );
}

const MAX_PHOTO_BYTES = 5 * 1024 * 1024;

function CardForm({
  type,
  existing,
  onSaved,
}: {
  readonly type: "Multisport" | "Meditopia";
  readonly existing?: CardDto;
  readonly onSaved: () => Promise<void>;
}) {
  const api = useApi();
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [cardNumber, setCardNumber] = useState(existing?.cardNumber ?? "");
  const [companyName, setCompanyName] = useState(existing?.companyName ?? "");
  const [expiryDate, setExpiryDate] = useState(existing?.expiryDate ?? "");
  const [photoName, setPhotoName] = useState<string | null>(null);
  const [photo, setPhoto] = useState<{ base64: string; contentType: string } | null>(null);
  const [fieldError, setFieldError] = useState<string | null>(null);

  const upsert = useMutation({
    mutationFn: () => {
      setFieldError(null);
      if (type === "Multisport" && !/^\d+$/.test(cardNumber.trim())) {
        throw new Error("Multisport kart numarası yalnızca rakamlardan oluşmalıdır.");
      }
      return api.member.upsertCard({
        type,
        cardNumber: type === "Multisport" ? cardNumber.trim() : null,
        companyName: companyName.trim(),
        expiryDate: expiryDate === "" ? null : expiryDate,
        status: "Active",
        photoBase64: photo?.base64 ?? null,
        photoContentType: photo?.contentType ?? null,
      });
    },
    onSuccess: async () => {
      toastSuccess("Kart bilgileriniz kaydedildi.");
      await onSaved();
    },
    onError: (error) => {
      if (error instanceof Error) setFieldError(error.message);
    },
  });

  function handlePhoto(file: File | undefined) {
    if (file === undefined) return;
    if (!["image/jpeg", "image/png", "image/gif"].includes(file.type)) {
      setFieldError("Fotoğraf JPG/PNG/GIF olmalıdır.");
      return;
    }
    if (file.size > MAX_PHOTO_BYTES) {
      setFieldError("Fotoğraf en fazla 5MB olabilir.");
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      const result = typeof reader.result === "string" ? reader.result : "";
      setPhoto({ base64: result.slice(result.indexOf(",") + 1), contentType: file.type });
      setPhotoName(file.name);
      setFieldError(null);
    };
    reader.readAsDataURL(file);
  }

  return (
    <form
      className="flex flex-col gap-3 rounded-lg border border-line p-4"
      onSubmit={(e) => {
        e.preventDefault();
        upsert.mutate();
      }}
      noValidate
    >
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold text-ink">{type}</p>
        {existing !== undefined ? (
          <Badge tone={existing.status === "Active" ? "success" : existing.status === "Passive" ? "neutral" : "danger"}>
            {CARD_STATUS_LABELS[existing.status] ?? existing.status}
          </Badge>
        ) : (
          <Badge>Kayıt yok</Badge>
        )}
      </div>

      {type === "Multisport" ? (
        <FormField label="Kart numarası" required>
          {(id) => (
            <Input
              id={id}
              inputMode="numeric"
              value={cardNumber}
              onChange={(e) => setCardNumber(e.target.value.replace(/\D/g, ""))}
            />
          )}
        </FormField>
      ) : null}

      <FormField label={type === "Meditopia" ? "Şirket adı" : "Şirket adı (opsiyonel)"} required={type === "Meditopia"}>
        {(id) => <Input id={id} value={companyName} onChange={(e) => setCompanyName(e.target.value)} />}
      </FormField>

      <FormField label="Son geçerlilik tarihi (opsiyonel)">
        {(id) => (
          <Input id={id} type="date" value={expiryDate} onChange={(e) => setExpiryDate(e.target.value)} />
        )}
      </FormField>

      <div className="flex items-center gap-2 text-[13px] text-ink-2">
        <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/gif" hidden onChange={(e) => handlePhoto(e.target.files?.[0])} />
        <Button variant="secondary" size="sm" onClick={() => fileRef.current?.click()}>
          Fotoğraf seç
        </Button>
        {photoName !== null ? <span className="max-w-[140px] truncate">{photoName}</span> : null}
        {existing?.hasPhoto === true && photo === null ? <span className="text-xs text-ink-3">(kayıtlı fotoğraf var)</span> : null}
      </div>

      {fieldError !== null ? (
        <p role="alert" className="text-sm text-danger">{fieldError}</p>
      ) : null}
      <div>
        <Button type="submit" size="sm" pending={upsert.isPending}>Kaydet</Button>
      </div>
    </form>
  );
}

function DangerZone() {
  const api = useApi();
  const [open, setOpen] = useState(false);
  const [password, setPassword] = useState("");

  const remove = useMutation({
    mutationFn: () => api.member.deleteAccount(password),
    onSuccess: () => {
      window.location.assign("/giris");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Hesap silinemedi."),
  });

  return (
    <>
      <SectionCard title="Hesabı sil" className="border-danger/40">
        <p className="mb-3 text-sm text-ink-2">
          Hesabınız, randevularınız, paketleriniz, mesajlarınız ve hareket kayıtlarınız
          <strong className="text-danger"> geri dönüşsüz</strong> olarak silinir.
        </p>
        <Button variant="danger" size="sm" onClick={() => setOpen(true)}>
          Hesabımı kalıcı olarak sil
        </Button>
      </SectionCard>

      <Dialog
        open={open}
        onClose={() => {
          setOpen(false);
          setPassword("");
        }}
        title="Hesap silinsin mi?"
        description="Bu işlem geri alınamaz. Onaylamak için parolanızı girin."
        size="sm"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setOpen(false)}>
              Vazgeç
            </Button>
            <Button variant="danger" size="sm" pending={remove.isPending} onClick={() => remove.mutate()}>
              Kalıcı olarak sil
            </Button>
          </>
        }
      >
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Parolanız"
          aria-label="Parola onayı"
          className="h-10 w-full rounded-md border border-line px-3 text-sm"
        />
      </Dialog>
    </>
  );
}
