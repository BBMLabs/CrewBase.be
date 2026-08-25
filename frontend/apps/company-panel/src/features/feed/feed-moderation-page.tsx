import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { CommentDto, ParticipantDto, PostDto } from "@crewbase/api-types";
import {
  AsyncBoundary,
  Badge,
  Button,
  ConfirmDialog,
  Dialog,
  EmptyState,
  MEDIA_KIND_LABELS,
  PageHeader,
  SectionCard,
} from "@crewbase/design-system";
import { useApi } from "../../app/api-context";
import { panelKeys } from "../../shared/query-keys";
import { useSessionStore } from "../auth/session-store";
import { formatIsoDateTr } from "../../shared/dates";
import { toastError, toastSuccess } from "../../shared/toast-store";

const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const MAX_VIDEO_BYTES = 25 * 1024 * 1024;

const IMAGE_TYPES = new Set(["image/jpeg", "image/png", "image/gif", "image/webp"]);
const VIDEO_TYPES = new Set(["video/mp4", "video/webm", "video/quicktime"]);

export function FeedModerationPage() {
  return (
    <div className="flex flex-col gap-5">
      <PageHeader
        title="Kulüp Akışı"
        description="Üyelerin gördüğü akışı kulüp adına besleyin ve moderasyon yapın."
      />
      <ClubComposer />
      <PostList />
    </div>
  );
}

function ClubComposer() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [body, setBody] = useState("");
  const [isEvent, setIsEvent] = useState(false);
  const [eventTitle, setEventTitle] = useState("");
  const [eventDate, setEventDate] = useState("");
  const [media, setMedia] = useState<{ base64: string; contentType: string; name: string } | null>(null);

  const publish = useMutation({
    mutationFn: () =>
      api.company.createClubPost({
        body: body.trim(),
        mediaBase64: media?.base64 ?? null,
        mediaContentType: media?.contentType ?? null,
        isEvent,
        eventTitle: isEvent ? eventTitle.trim() || null : null,
        eventDate: isEvent && eventDate !== "" ? eventDate : null,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.feed.root(companyId) });
      setBody("");
      setIsEvent(false);
      setEventTitle("");
      setEventDate("");
      setMedia(null);
      toastSuccess("Kulüp adına paylaşıldı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paylaşım yayınlanamadı."),
  });

  function handleFile(file: File | undefined) {
    if (file === undefined) return;
    if (IMAGE_TYPES.has(file.type) && file.size <= MAX_IMAGE_BYTES) {
      void readFile(file);
      return;
    }
    if (VIDEO_TYPES.has(file.type) && file.size <= MAX_VIDEO_BYTES) {
      void readFile(file);
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
      const base64 = result.includes(",") ? result.slice(result.indexOf(",") + 1) : result;
      setMedia({ base64, contentType: file.type, name: file.name });
    };
    reader.readAsDataURL(file);
  }

  return (
    <SectionCard title="Kulüp adına paylaş">
      <form className="flex flex-col gap-3" onSubmit={(e) => { e.preventDefault(); publish.mutate(); }} noValidate>
        <textarea
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows={3}
          placeholder={"Kulüpten bir haber yazın…"}
          aria-label="Paylaşım metni"
          className="w-full rounded-md border border-line bg-surface px-3 py-2 text-sm placeholder:text-ink-3 focus:border-brand-500 focus-visible:outline-none"
        />
        <div className="flex flex-wrap items-center gap-3 text-[13px] text-ink-2">
          <label className="flex items-center gap-1.5">
            <input type="checkbox" checked={isEvent} onChange={(e) => setIsEvent(e.target.checked)} className="size-4 accent-brand-600" />
            Etkinlik olarak işaretle
          </label>
          {isEvent ? (
            <>
              <input
                value={eventTitle}
                onChange={(e) => setEventTitle(e.target.value)}
                placeholder="Etkinlik başlığı"
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
          <input
            ref={fileRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp,video/mp4,video/webm,video/quicktime"
            hidden
            onChange={(e) => handleFile(e.target.files?.[0])}
          />
          <Button variant="secondary" size="sm" onClick={() => fileRef.current?.click()}>
            {media !== null ? "Medyayı değiştir" : "Medya ekle"}
          </Button>
          {media !== null ? (
            <span className="flex items-center gap-2">
              <Badge tone="info">{MEDIA_KIND_LABELS.Image !== "" ? (media.contentType.startsWith("video/") ? "Video" : "Görsel") : ""}</Badge>
              <span className="max-w-[180px] truncate">{media.name}</span>
              <button type="button" className="text-xs text-danger hover:underline" onClick={() => setMedia(null)}>
                kaldır
              </button>
            </span>
          ) : null}
          <Button type="submit" size="sm" pending={publish.isPending} disabled={body.trim() === ""} className="ml-auto">
            Yayınla
          </Button>
        </div>
      </form>
    </SectionCard>
  );
}

function PostList() {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const [expanded, setExpanded] = useState<string | null>(null);

  const feed = useQuery({ queryKey: panelKeys.feed.list(companyId), queryFn: () => api.company.feed() });

  return (
    <AsyncBoundary state={feed} isEmpty={(feed.data?.length ?? 0) === 0}>
      {(feed.data?.length ?? 0) === 0 ? (
        <EmptyState title="Akış boş" description="İlk paylaşımı kulüp adına yayınlayın." />
      ) : (
        <ol className="flex flex-col gap-3">
          {(feed.data ?? []).map((post) => (
            <li key={post.id}>
              <PostCard post={post} expanded={expanded === post.id} onToggle={() => setExpanded(expanded === post.id ? null : post.id)} />
            </li>
          ))}
        </ol>
      )}
    </AsyncBoundary>
  );
}

function PostCard({
  post,
  expanded,
  onToggle,
}: {
  readonly post: PostDto;
  readonly expanded: boolean;
  readonly onToggle: () => void;
}) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const queryClient = useQueryClient();
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [mediaData, setMediaData] = useState<{ base64: string; contentType: string } | null>(null);
  const [lightbox, setLightbox] = useState(false);

  function loadMedia() {
    void api.company.postMedia(post.id).then(
      (media) => {
        if (media !== null) {
          setMediaData(media);
          setLightbox(true);
        } else {
          toastError("Bu paylaşımda medya yok.");
        }
      },
      (error: unknown) => toastError(error instanceof Error ? error.message : "Medya yüklenemedi."),
    );
  }

  const remove = useMutation({
    mutationFn: () => api.company.deleteAnyPost(post.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: panelKeys.feed.root(companyId) });
      toastSuccess("Paylaşım kaldırıldı.");
    },
    onError: (error) => toastError(error instanceof Error ? error.message : "Paylaşım kaldırılamadı."),
  });

  return (
    <article className="rounded-lg border border-line bg-surface shadow-xs">
      <div className="px-4 py-3">
        <div className="mb-2 flex flex-wrap items-center gap-2 text-[13px] text-ink-3">
          <span className="font-medium text-ink">{post.authorName}</span>
          {post.isClubPost ? <Badge tone="accent">Kulüp</Badge> : null}
          <time dateTime={post.createdAtUtc}>{formatIsoDateTr(post.createdAtUtc.slice(0, 10))}</time>
          {post.isEvent ? (
            <Badge tone="info">
              Etkinlik{post.eventTitle !== null ? ` · ${post.eventTitle}` : ""}
              {post.eventDate !== null ? ` · ${formatIsoDateTr(post.eventDate)}` : ""}
            </Badge>
          ) : null}
          {post.mediaKind !== "None" ? <Badge>{MEDIA_KIND_LABELS[post.mediaKind]}</Badge> : null}
        </div>
        <p className="whitespace-pre-wrap text-sm text-ink">{post.body}</p>

        {post.mediaKind !== "None" && mediaData === null ? (
          <Button variant="secondary" size="sm" className="mt-3" onClick={loadMedia}>
            Medyayı görüntüle
          </Button>
        ) : null}
        {mediaData !== null ? (
          <button
            type="button"
            onClick={() => setLightbox(true)}
            aria-label="Medyayı büyüt"
            className="mt-3 block cursor-zoom-in text-left"
          >
            {mediaData.contentType.startsWith("video/") ? (
              <video muted className="max-h-48 w-full rounded-md border border-line bg-black" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
            ) : (
              <img alt="Paylaşım görseli" className="max-h-48 w-auto rounded-md border border-line object-contain" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
            )}
          </button>
        ) : null}

        <div className="mt-3 flex flex-wrap items-center gap-4 text-xs text-ink-3">
          <span>❤ {post.likeCount}</span>
          <span>💬 {post.commentCount}</span>
          {post.isEvent ? <span>👥 {post.participantCount}</span> : null}
          <button type="button" className="text-brand-600 hover:underline" onClick={onToggle} aria-expanded={expanded}>
            {expanded ? "kapat" : "ayrıntılar"}
          </button>
          <Button variant="ghost" size="sm" className="!px-2 !py-1 text-danger" pending={remove.isPending} onClick={() => setConfirmDelete(true)}>
            Kaldır
          </Button>
        </div>
      </div>

      {expanded ? (
        <div className="border-t border-line px-4 py-3">
          <CommentsSection postId={post.id} />
          {post.isEvent ? <ParticipantsSection postId={post.id} /> : null}
        </div>
      ) : null}

      {/* Media lightbox */}
      {mediaData !== null ? (
        <Dialog open={lightbox} onClose={() => setLightbox(false)} title="Medya" size="lg">
          {mediaData.contentType.startsWith("video/") ? (
            <video controls autoPlay className="max-h-[70vh] w-full rounded-md bg-black" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
          ) : (
            <img alt="Paylaşım görseli (büyük)" className="max-h-[70vh] w-full rounded-md object-contain" src={`data:${mediaData.contentType};base64,${mediaData.base64}`} />
          )}
        </Dialog>
      ) : null}

      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        title="Paylaşım kaldırılsın mı?"
        description="Bu işlem geri alınamaz; paylaşım akıştan tamamen silinir."
        confirmLabel="Kaldır"
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

function CommentsSection({ postId }: { readonly postId: string }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const comments = useQuery({
    queryKey: panelKeys.feed.comments(companyId, postId),
    queryFn: () => api.company.postComments(postId),
  });

  return (
    <section aria-label="Yorumlar" className="mb-4">
      <h4 className="mb-2 text-xs font-semibold uppercase tracking-wide text-ink-3">Yorumlar</h4>
      {comments.isPending ? (
        <p className="text-sm text-ink-3">Yükleniyor…</p>
      ) : (comments.data?.length ?? 0) === 0 ? (
        <p className="text-sm text-ink-3">Yorum yok.</p>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {(comments.data as readonly CommentDto[]).map((comment) => (
            <li key={comment.id} className="text-sm">
              <span className="font-medium text-ink">{comment.authorName}</span>{" "}
              <span className="text-ink-2">{comment.body}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function ParticipantsSection({ postId }: { readonly postId: string }) {
  const api = useApi();
  const companyId = useSessionStore((s) => s.companyId);
  const participants = useQuery({
    queryKey: panelKeys.feed.participants(companyId, postId),
    queryFn: () => api.company.postParticipants(postId),
  });

  return (
    <section aria-label="Katılımcılar">
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
    </section>
  );
}
