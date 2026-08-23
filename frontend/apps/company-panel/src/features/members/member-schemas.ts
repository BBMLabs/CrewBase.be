import { z } from "zod";

/** Mirrors CreateMemberRequest constraints verified in backend (phone ≤20, name ≤200). */
export const createMemberSchema = z.object({
  fullName: z.string().trim().min(1, "Ad zorunludur.").max(200),
  phone: z.string().trim().min(1, "Telefon zorunludur.").max(20),
  email: z.string().email("Geçerli bir e-posta girin.").optional().or(z.literal("")),
  level: z.string().regex(/^\d$/, "0-10 arası bir seviye seçin."),
});

export type CreateMemberForm = {
  readonly fullName: string;
  readonly phone: string;
  readonly email: string;
  readonly level: string;
};
