import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import type { FriendDto, MessageDto } from "@crewbase/api-types";
import { Badge, Button, FormField, Input, PageHeader } from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { useSessionStore } from "../auth/session-store";
import { toastError, toastSuccess } from "../../shared/toast-store";
import { chatConnectionManager } from "./realtime/chat-connection-manager";

export function FriendsPage() {
  const api = useApi();
  const [addCode, setAddCode] = useState("");

  const friends = useQuery({ queryKey: ["member", "friends"], queryFn: () => api.member.friends() });
  const myCode = useQuery({ queryKey: ["member", "code"], queryFn: () => api.member.myCode() });

  const incoming = (friends.data ?? []).filter((f) => f.direction === "incoming");
  const outgoing = (friends.data ?? []).filter((f) => f.direction === "outgoing");
  const established = (friends.data ?? []).filter((f) => f.direction === "friend");

  const add = useMutation({
    mutationFn: () => api.member.addFriend({ memberCode: addCode.trim() }),
    onSuccess: async () => {
      setAddCode("");
      toastSuccess("ArkadaÅŸlÄ±k isteÄŸi gÃ¶nderildi.");
      await friends.refetch();
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Ä°stek gÃ¶nderilemedi."),
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="ArkadaÅŸlar" description="Kodunuzu paylaÅŸÄ±n; sohbet yalnÄ±zca kabul edilmiÅŸ arkadaÅŸlarla aÃ§Ä±lÄ±r." />

      <section aria-label="Benim kodum" className="flex items-center justify-between gap-3 rounded-lg border border-line bg-surface px-4 py-3 shadow-xs">
        <div>
          <p className="text-xs text-ink-3">Ãœye kodunuz</p>
          <p className="font-mono text-lg font-semibold tracking-wider text-brand-700">{myCode.data ?? "â€¦"}</p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => {
            if (myCode.data !== undefined) void navigator.clipboard?.writeText(myCode.data);
            toastSuccess("Kod kopyalandÄ±.");
          }}
        >
          Kopyala
        </Button>
      </section>

      {incoming.length > 0 ? (
        <section aria-label="Gelen istekler">
          <h2 className="mb-2 text-sm font-semibold text-ink">Gelen istekler</h2>
          <ul className="flex flex-col gap-2">
            {incoming.map((friend) => (
              <li key={friend.friendshipId}>
                <FriendRow friend={friend} actions />
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <section aria-label="ArkadaÅŸ ekle">
        <form
          className="flex items-end gap-2"
          onSubmit={(e) => {
            e.preventDefault();
            if (addCode.trim() !== "") add.mutate();
          }}
          noValidate
        >
          <FormField label="Kodla arkadaÅŸ ekle" className="w-full max-w-xs">
            {(id) => (
              <Input
                id={id}
                value={addCode}
                onChange={(e) => setAddCode(e.target.value.toUpperCase())}
                placeholder="KRK-XXXXXX"
                className="font-mono"
              />
            )}
          </FormField>
          <Button type="submit" size="sm" pending={add.isPending} disabled={addCode.trim() === ""}>
            Ä°stek gÃ¶nder
          </Button>
        </form>
      </section>

      <section aria-label="ArkadaÅŸlarÄ±m">
        <h2 className="mb-2 text-sm font-semibold text-ink">ArkadaÅŸlarÄ±m</h2>
        {established.length === 0 ? (
          <p className="rounded-md border border-dashed border-line bg-surface px-4 py-6 text-center text-sm text-ink-3">
            HenÃ¼z arkadaÅŸÄ±nÄ±z yok.
          </p>
        ) : (
          <ul className="flex flex-col gap-2">
            {established.map((friend) => (
              <li key={friend.friendshipId}>
                <FriendRow friend={friend} />
              </li>
            ))}
          </ul>
        )}
      </section>

      {outgoing.length > 0 ? (
        <section aria-label="GÃ¶nderilen istekler">
          <h2 className="mb-2 text-sm font-semibold text-ink">Bekleyen istekleriniz</h2>
          <ul className="flex flex-col gap-2 opacity-75">
            {outgoing.map((friend) => (
              <li key={friend.friendshipId}>
                <FriendRow friend={friend} />
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}

function FriendRow({ friend, actions = false }: { readonly friend: FriendDto; readonly actions?: boolean }) {
  const api = useApi();
  const queryClient = useQueryClient();

  const respond = useMutation({
    mutationFn: (accept: boolean) =>
      accept ? api.member.acceptFriend(friend.friendshipId) : api.member.rejectFriend(friend.friendshipId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["member", "friends"] });
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Ä°ÅŸlem baÅŸarÄ±sÄ±z."),
  });

  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-lg border border-line bg-surface px-4 py-3 shadow-xs">
      <div className="min-w-[140px] flex-1">
        <p className="text-sm font-medium text-ink">
          {friend.fullName}
          {friend.unreadCount > 0 ? <Badge tone="danger" className="ml-2">{friend.unreadCount} yeni</Badge> : null}
        </p>
        {friend.memberCode !== null ? (
          <p className="font-mono text-xs text-ink-3">{friend.memberCode}</p>
        ) : null}
      </div>
      {actions ? (
        <div className="flex gap-2">
          <Button size="sm" pending={respond.isPending} onClick={() => respond.mutate(true)}>
            Kabul et
          </Button>
          <Button variant="secondary" size="sm" onClick={() => respond.mutate(false)}>
            Reddet
          </Button>
        </div>
      ) : friend.direction === "friend" ? (
        <Link to={`/sohbet/${friend.customerId}`} className="text-sm text-brand-600 hover:underline">
          Sohbet â†’
        </Link>
      ) : (
        <Badge tone="warning">bekliyor</Badge>
      )}
    </div>
  );
}

/** Chat thread â€” route /sohbet/:customerId */
export function ChatThreadPage() {
  const { customerId = "" } = useParams<{ customerId: string }>();
  const api = useApi();
  const queryClient = useQueryClient();
  const sessionMemberId = useSessionStore((s) => s.member?.customerId ?? null);
  const [draft, setDraft] = useState("");
  const [sendFailed, setSendFailed] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<
    "idle" | "connecting" | "connected" | "reconnecting" | "stopped"
  >("idle");
  const bottomRef = useRef<HTMLDivElement | null>(null);

  const conversation = useQuery({
    queryKey: ["member", "messages", customerId],
    queryFn: () => api.member.conversation(customerId),
  });

  const friends = useQuery({ queryKey: ["member", "friends"], queryFn: () => api.member.friends() });
  const friend = (friends.data ?? []).find((f) => f.customerId === customerId);

  const send = useMutation({
    mutationFn: () => api.member.sendMessage(customerId, { body: draft.trim() }),
    onSuccess: async (message: MessageDto) => {
      setDraft("");
      setSendFailed(false);
      await queryClient.invalidateQueries({ queryKey: ["member", "messages", customerId] });
      await queryClient.invalidateQueries({ queryKey: ["member", "friends"] });
      void message;
    },
    onError: () => {
      // Draft is intentionally retained; inline retry UI appears under the composer.
      setSendFailed(true);
    },
  });

  useEffect(() => {
    const off = chatConnectionManager.onStatus(setConnectionStatus);
    return off;
  }, []);

  // Realtime receive + lifecycle
  useEffect(() => {
    chatConnectionManager.configure({
      apiBaseUrl: new URL(".", globalThis.location.href).origin.includes("localhost")
        ? (import.meta.env["VITE_API_BASE_URL"] as string | undefined) ?? "http://localhost:5283"
        : globalThis.location.origin,
      getAccessToken: () => api.session.getAccessToken(),
      onMessage: (message) => {
        if (message.senderId !== customerId && message.recipientId !== customerId) return;
        queryClient.setQueryData<readonly MessageDto[]>(["member", "messages", customerId], (old) => {
          const rows = old ?? [];
          if (rows.some((m) => m.id === message.id)) return rows;
          return [...rows, message].sort((a, b) => a.sentAtUtc.localeCompare(b.sentAtUtc));
        });
      },
      onReconnect: () => {
        void queryClient.invalidateQueries({ queryKey: ["member", "messages", customerId] });
      },
      onFatal: () => undefined,
    });

    void chatConnectionManager.acquire();
    return () => {
      chatConnectionManager.release();
    };
  }, [api, customerId, queryClient]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ block: "end" });
  }, [conversation.data?.length]);

  return (
    <div className="flex min-h-[70dvh] flex-col">
      <header className="mb-3 flex items-center gap-2">
        <Link to="/arkadaslar" className="text-sm text-brand-600 hover:underline">← Arkadaşlar</Link>
        <h1 className="font-display text-base font-semibold text-ink">{friend?.fullName ?? "Sohbet"}</h1>
        {connectionStatus === "reconnecting" || connectionStatus === "connecting" ? (
          <span className="ml-auto rounded-full bg-warning-bg px-2.5 py-0.5 text-[11px] font-semibold text-warning">
            Bağlantı kuruluyor…
          </span>
        ) : null}
        {connectionStatus === "stopped" || connectionStatus === "reconnecting" ? (
          <span className="ml-auto text-[11px] text-ink-3">Bağlantı yeniden kuruluyor…</span>
        ) : null}
      </header>

      <ol className="flex flex-1 flex-col gap-2 overflow-y-auto rounded-lg border border-line bg-surface p-4">
        {(conversation.data ?? []).map((message) => {
          const mine = message.senderId === sessionMemberId;
          return (
            <li key={message.id} className={mine ? "self-end" : "self-start"}>
              <div
                className={[
                  "max-w-[80%] rounded-2xl px-3.5 py-2 text-sm",
                  mine ? "rounded-br-md bg-brand-600 text-white" : "rounded-bl-md bg-surface-2 text-ink",
                ].join(" ")}
              >
                <p className="whitespace-pre-wrap break-words">{message.body}</p>
                <p className={`mt-0.5 text-right font-mono text-[10px] ${mine ? "text-white/70" : "text-ink-3"}`}>
                  {message.sentAtUtc.slice(11, 16)}
                </p>
              </div>
            </li>
          );
        })}
        {(conversation.data?.length ?? 0) === 0 && !conversation.isPending ? (
          <li className="py-8 text-center text-sm text-ink-3">HenÃ¼z mesaj yok. Ä°lk mesajÄ± siz yazÄ±n.</li>
        ) : null}
        <div ref={bottomRef} />
      </ol>

      <form
        className="mt-3 flex gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          if (draft.trim() !== "") send.mutate();
        }}
        noValidate
      >
        <Input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          maxLength={2000}
          placeholder="Mesaj yazÄ±nâ€¦"
          aria-label="Mesaj"
        />
        <Button type="submit" pending={send.isPending} disabled={draft.trim() === ""}>
          Gönder
        </Button>
      </form>
      {sendFailed ? (
        <p role="alert" className="mt-1.5 text-xs font-medium text-danger">
          Mesaj gönderilemedi — taslak korundu, tekrar deneyin.
        </p>
      ) : null}
    </div>
  );
}
