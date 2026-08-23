import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ParticipantDto, PostDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  Button,
  ConfirmDialog,
  Dialog,
  EmptyState,
  Input,
  PageHeader,
  PennantStrip,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { formatIsoDateTr } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const MAX_VIDEO_BYTES = 25 * 1024 * 1024;
const IMAGE_TYPES = new Set(["image/jpeg", "image/png", "image/gif", "image/webp"]);
const VIDEO_TYPES = new Set(["video/mp4", "video/webm", "video/quicktime"]);

export function FeedPage() {
  return (
    <div className="flex flex-col gap-5">
      <PennantStrip className="mb-1 text-ink-3/70" />
      <PageHeader title="Kulüp Akışı" description="Kulüpten ve üyelerden son paylaşımlar." />
      <Composer />
      <PostList />
    </div>
  );
}

function Composer() {
  const api = useApi();
  const queryClient = useQueryClient();
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [body, setBody] = useState("");
  const [isEvent, setIsEvent] = useState(false);
  const [eventTitle, setEventTitle] = useState("");
  const [eventDate, setEventDate] = useState("");
  const [media, setMedia] = useState<{ base64: string; contentType: string; name: string } | null>(null);

  const publish = useMutation({
    mutationFn: () =>
      api.member.createPost({
        body: body.trim(),
        mediaBase64: media?.base64 ?? null,
        mediaContentType: media?.contentType ?? null,
        isEvent,
        eventTitle: isEvent ? eventTitle.trim() || null : null,
        eventDate: isEvent && eventDate !== "" ? eventDate : null,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["member", "feed"] });
      setBody("");
      setIsEvent(false);
      setEventTitle("");
      setEventDate("");
      setMedia(null);
      toastSuccess("Paylaşımınız yayınlandı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paylaşım yayınlanamadı."),
  });

  function handleFile(file: File | undefined) {
    if (file === undefined) return;
    if (IMAGE_TYPES.has(file.type) && file.size <= MAX_IMAGE_BYTES) {
      readFile(file);
      return;
    }
    if (VIDEO_TYPES.has(file.type) && file.size <= MAX_VIDEO_BYTES) {
      readFile(file);
      return;
    }
    toastError(
      IMAGE_TYPES.has(file.type)
        ? "Görsel en fazla 5MB olabilir."
        : VIDEO_TYPES.has(file.type)
          ? "Video en fazla 25MB olabilir."
          : "Görsel JPG/PNG/GIF/WebP, video MP4/WebM/QuickTime olmalıdır.",
    );
  }

  function readFile(file: File) {
    const reader = new FileReader();
    reader.onload = () => {
      const result = typeof reader.result === "string" ? reader.result : "";
      setMedia({ base64: result.slice(result.indexOf(",") + 1), contentType: file.type, name: file.name });
    };
    reader.readAsDataURL(file);
  }

  return (
    <form
      className="flex flex-col gap-3 rounded-lg border border-line bg-surface p-4 shadow-xs"
      onSubmit={(e) => {
        e.preventDefault();
        publish.mutate();
      }}
      noValidate
    >
      <textarea
        value={body}
        onChange={(e) => setBody(e.target.value)}
        rows={3}
        placeholder="Ne paylaşmak istersiniz?"
        aria-label="Paylaşım metni"
        className="w-full rounded-md border border-line bg-surface px-3 py-2 text-sm placeholder:text-ink-3 focus:border-brand-500 focus-visible:outline-none"
      />
      <div className="flex flex-wrap items-center gap-3 text-[13px] text-ink-2">
        <label className="flex items-center gap-1.5">
          <input type="checkbox" checked={isEvent} onChange={(e) => setIsEvent(e.target.checked)} className="size-4 accent-brand-600" />
          Etkinlik
        </label>
        {isEvent ? (
          <>
            <input
              value={eventTitle}
              onChange={(e) => setEventTitle(e.target.value)}
              placeholder="Başlık"
              aria-label="Etkinlik başlığı"
              className="h-8 rounded-sm border border-line px-2 text-sm"
            />
            <input
              type="date"
              value={eventDate}
              onChange={(e) => setEventDate(e.target.value)}
              aria-label="Etkinlik tarihi"
              className="h-8 rounded-sm border border-line px-2 text-sm"
            />
          </>
        ) : null}
        <input ref={fileRef} type="file" accept="image/jpeg,image/png,image/gif,image/webp,video/mp4,video/webm,video/quicktime" hidden onChange={(e) => handleFile(e.target.files?.[0])} />
        <Button variant="secondary" size="sm" onClick={() => fileRef.current?.click()}>
          {media !== null ? "Medyayı değiştir" : "Medya ekle"}
        </Button>
        {media !== null ? (
          <span className="flex max-w-[200px] items-center gap-2 truncate">
            <Badge tone="info">{media.contentType.startsWith("video/") ? "Video" : "Görsel"}</Badge>
            <span className="truncate">{media.name}</span>
            <button type="button" className="text-xs text-danger hover:underline" onClick={() => setMedia(null)}>
              kaldır
            </button>
          </span>
        ) : null}
        <Button type="submit" size="sm" pending={publish.isPending} disabled={body.trim() === ""} className="ml-auto">
          Paylaş
        </Button>
      </div>
    </form>
  );
}

function PostList() {
  const api = useApi();
  const feed = useQuery({ queryKey: ["member", "feed"], queryFn: () => api.member.feed() });

  return (
    <AsyncBoundary state={feed} isEmpty={(feed.data?.length ?? 0) === 0}>
      {(feed.data?.length ?? 0) === 0 ? (
        <EmptyState title="Akış boş" description="İlk paylaşımı siz yapın." />
      ) : (
        <ol className="flex flex-col gap-3">
          {(feed.data ?? []).map((post) => (
            <li key={post.id}>
              <PostCard post={post} />
            </li>
          ))}
        </ol>
      )}
    </AsyncBoundary>
  );
}

function PostCard({ post }: { readonly post: PostDto }) {
  const api = useApi();
  const queryClient = useQueryClient();
  const [expanded, setExpanded] = useState(false);
  const [showParticipants, setShowParticipants] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [mediaData, setMediaData] = useState<{ base64: string; contentType: string } | null>(null);
  const [lightbox, setLightbox] = useState(false);

  const patchPost = (updater: (post: PostDto) => PostDto) => {
    queryClient.setQueryData<readonly PostDto[]>(["member", "feed"], (rows) =>
      (rows ?? []).map((row) => (row.id === post.id ? updater(row) : row)),
    );
  };

  const toggleLike = useMutation({
    mutationFn: () => api.member.toggleLike(post.id),
    onSuccess: (result) => patchPost((p) => ({ ...p, likedByMe: result.active, likeCount: result.count })),
    onError: (error) => {
      toastError(error instanceof Error ? error.message : "Beğeni kaydedilemedi.");
      void queryClient.invalidateQueries({ queryKey: ["member", "feed"] });
    },
  });

  const toggleJoin = useMutation({
    mutationFn: () => api.member.toggleJoin(post.id),
    onSuccess: (result) => patchPost((p) => ({ ...p, joinedByMe: result.active, participantCount: result.count })),
    onError: (error) => {
      toastError(error instanceof Error ? error.message : "Katılım kaydedilemedi.");
      void queryClient.invalidateQueries({ queryKey: ["member", "feed"] });
    },
  });

  const remove = useMutation({
    mutationFn: () => api.member.deleteOwnPost(post.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["member", "feed"] });
      toastSuccess("Paylaşım silindi.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Silinemedi."),
  });

  function loadMedia() {
    void api.member.postMedia(post.id).then(
      (media) => {
        if (media !== null) setMediaData(media);
        else toastError("Bu paylaşımda medya yok.");
      },
      (error: unknown) => toastError(error instanceof Error ? error.message : "Medya yüklenemedi."),
    );
  }

  return (
    <article className="rounded-lg border border-line bg-surface shadow-xs">
      <header className="flex flex-wrap items-center gap-x-2 gap-y-1 px-4 pt-3 text-[13px] text-ink-3">
        <span className="font-medium text-ink">{post.authorName}</span>
        {post.isClubPost ? <Badge tone="accent">Kulüp</Badge> : null}
        <time dateTime={post.createdAtUtc}>{formatIsoDateTr(post.createdAtUtc.slice(0, 10))}</time>
        {post.isEvent ? (
          <Badge tone="info">
            Etkinlik{post.eventTitle !== null ? ` · ${post.eventTitle}` : ""}
            {post.eventDate !== null ? ` · ${formatIsoDateTr(post.eventDate)}` : ""}
          </Badge>
        ) : null}
        {post.isMine ? (
          <button type="button" className="ml-auto text-xs text-danger hover:underline" onClick={() => setConfirmDelete(true)}>
            sil
          </button>
        ) : null}
      </header>

      <div className="px-4 pb-3 pt-2">
        <p className="whitespace-pre-wrap text-sm text-ink">{post.body}</p>

        {post.mediaKind !== "None" && mediaData === null ? (
          <Button variant="secondary" size="sm" className="mt-3" onClick={loadMedia}>
            Medyayı yükle
          </Button>
        ) : null}
        {mediaData !== null ? (
          mediaData.contentType.startsWith("video/") ? (
            <video controls className="mt-3 max-h-80 w-full rounded-md bg-black" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
          ) : (
            <button type="button" onClick={() => setLightbox(true)} aria-label="Görseli büyüt" className="mt-3 block cursor-zoom-in">
              <img alt="Paylaşım görseli" className="max-h-64 w-auto rounded-md border border-line object-contain" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
            </button>
          )
        ) : null}
      </div>

      {/* Image lightbox */}
      {mediaData !== null && !mediaData.contentType.startsWith("video/") ? (
        <Dialog open={lightbox} onClose={() => setLightbox(false)} title="Görsel" size="lg">
          <img alt="Paylaşım görseli (büyük)" className="max-h-[70vh] w-full rounded-md object-contain" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
        </Dialog>
      ) : null}

      <footer className="flex flex-wrap items-center gap-2 border-t border-line px-4 py-2.5 text-[13px]">
        <Button
          variant="ghost"
          size="sm"
          pending={toggleLike.isPending}
          className={post.likedByMe ? "text-danger" : "text-ink-2"}
          onClick={() => toggleLike.mutate()}
        >
          {post.likedByMe ? "♥" : "♡"} {post.likeCount}
        </Button>
        <Button variant="ghost" size="sm" className="text-ink-2" onClick={() => setExpanded(!expanded)} aria-expanded={expanded}>
          💬 {post.commentCount}
        </Button>
        {post.isEvent ? (
          <>
            <Button
              variant="ghost"
              size="sm"
              pending={toggleJoin.isPending}
              className={post.joinedByMe ? "text-success" : "text-ink-2"}
              onClick={() => toggleJoin.mutate()}
            >
              {post.joinedByMe ? "✓ Katılıyorsun" : "Katıl"} · {post.participantCount}
            </Button>
            <Button variant="ghost" size="sm" className="text-ink-3" onClick={() => setShowParticipants(!showParticipants)}>
              katılımcılar
            </Button>
          </>
        ) : null}
        {!post.isMine && post.authorCustomerId !== null ? (
          <FollowButton customerId={post.authorCustomerId} following={post.followingAuthor} />
        ) : null}
      </footer>

      {expanded ? <CommentsSection postId={post.id} /> : null}
      {showParticipants && post.isEvent ? <ParticipantsSection postId={post.id} /> : null}

      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        title="Paylaşım silinsin mi?"
        description="Bu işlem geri alınamaz."
        confirmLabel="Sil"
        danger
        pending={remove.isPending}
        onConfirm={() => {
          setConfirmDelete(false);
          remove.mutate();
        }}
      />
    </article>
  );
}

function FollowButton({ customerId, following }: { readonly customerId: string; readonly following: boolean }) {
  const api = useApi();
  const follow = useMutation({
    mutationFn: () => api.member.toggleFollow(customerId),
    onError: (error) => toastError(error instanceof Error ? error.message : "Takip işlemi başarısız."),
  });

  return (
    <Button
      variant="ghost"
      size="sm"
      pending={follow.isPending}
      className={following ? "text-brand-700" : "text-ink-3"}
      onClick={() => follow.mutate()}
    >
      {following ? "Takiptesin ✓" : "Takip et"}
    </Button>
  );
}

function CommentsSection({ postId }: { readonly postId: string }) {
  const api = useApi();
  const queryClient = useQueryClient();
  const comments = useQuery({
    queryKey: ["member", "feed-comments", postId],
    queryFn: () => api.member.comments(postId),
  });
  const [draft, setDraft] = useState("");

  const add = useMutation({
    mutationFn: () => api.member.addComment(postId, { body: draft.trim() }),
    onSuccess: async () => {
      setDraft("");
      await queryClient.invalidateQueries({ queryKey: ["member", "feed-comments", postId] });
      await queryClient.invalidateQueries({ queryKey: ["member", "feed"] });
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Yorum eklenemedi."),
  });

  return (
    <div className="border-t border-line px-4 py-3">
      {(comments.data ?? []).map((comment) => (
        <p key={comment.id} className="mb-1 text-sm">
          <span className="font-medium text-ink">{comment.authorName}</span>{" "}
          <span className="text-ink-2">{comment.body}</span>
        </p>
      ))}
      {comments.isPending ? <p className="text-xs text-ink-3">Yorumlar yükleniyor…</p> : null}
      <form
        className="mt-2 flex gap-2"
        onSubmit={(e) => {
          e.preventDefault();
          if (draft.trim() !== "") add.mutate();
        }}
        noValidate
      >
        <Input value={draft} onChange={(e) => setDraft(e.target.value)} placeholder="Yorum yaz…" aria-label="Yorum" maxLength={1000} />
        <Button type="submit" size="sm" pending={add.isPending} disabled={draft.trim() === ""}>
          Gönder
        </Button>
      </form>
    </div>
  );
}

function ParticipantsSection({ postId }: { readonly postId: string }) {
  const api = useApi();
  const participants = useQuery({
    queryKey: ["member", "participants", postId],
    queryFn: () => api.member.participants(postId),
  });

  return (
    <div className="border-t border-line px-4 py-3">
      <h4 className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-3">Katılımcılar</h4>
      {participants.isPending ? (
        <p className="text-sm text-ink-3">Yükleniyor…</p>
      ) : (participants.data?.length ?? 0) === 0 ? (
        <p className="text-sm text-ink-3">Henüz katılım yok.</p>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {(participants.data as readonly ParticipantDto[]).map((participant, index) => (
            <li key={`${participant.fullName}-${index}`} className="rounded-full bg-surface-2 px-3 py-1 text-xs text-ink-2">
              {participant.fullName}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
