/** Member self-service — full verified /api/v1/member surface. */
import type {
  CardDto,
  CardUpsertRequest,
  CommentCreateRequest,
  CommentDto,
  ConsentEntry,
  ConsentStateDto,
  MemberAppointmentDto,
  MemberCodeResponse,
  MemberDeleteRequest,
  MemberDto,
  MemberUpdateRequest,
  MessageDto,
  MessageSendRequest,
  MyConsentsResponse,
  OtpPurpose,
  ParticipantDto,
  PostCreatedResponse,
  PostCreateRequest,
  PostDto,
  PostMediaDto,
  ToggleResult,
} from "@crewbase/api-types";
import type { FriendAddRequest, FriendDto, CustomerPackageDto } from "@crewbase/api-types";
import type { ApiClient } from "../http/api-client";
import type { BookAppointmentResponse, MemberBookingRequest } from "@crewbase/api-types";

export interface MemberService {
  profile(): Promise<MemberDto>;
  updateProfile(request: MemberUpdateRequest): Promise<MemberDto>;
  deleteAccount(password: string): Promise<void>;

  requestOtp(purpose: OtpPurpose): Promise<void>;
  verifyOtp(purpose: OtpPurpose, code: string): Promise<MemberDto>;

  appointments(): Promise<readonly MemberAppointmentDto[]>;
  book(request: MemberBookingRequest): Promise<BookAppointmentResponse>;
  cancelAppointment(appointmentId: string): Promise<void>;
  packages(): Promise<readonly CustomerPackageDto[]>;

  consents(): Promise<MyConsentsResponse>;
  submitConsents(entries: readonly ConsentEntry[]): Promise<readonly ConsentStateDto[]>;

  cards(): Promise<readonly CardDto[]>;
  upsertCard(request: CardUpsertRequest): Promise<CardDto>;

  myCode(): Promise<string>;
  friends(): Promise<readonly FriendDto[]>;
  addFriend(request: FriendAddRequest): Promise<FriendDto>;
  acceptFriend(friendshipId: string): Promise<void>;
  rejectFriend(friendshipId: string): Promise<void>;
  conversation(friendCustomerId: string, take?: number): Promise<readonly MessageDto[]>;
  sendMessage(friendCustomerId: string, request: MessageSendRequest): Promise<MessageDto>;

  feed(): Promise<readonly PostDto[]>;
  createPost(request: PostCreateRequest): Promise<PostCreatedResponse>;
  postMedia(postId: string): Promise<PostMediaDto | null>;
  deleteOwnPost(postId: string): Promise<void>;
  toggleLike(postId: string): Promise<ToggleResult>;
  comments(postId: string): Promise<readonly CommentDto[]>;
  addComment(postId: string, request: CommentCreateRequest): Promise<CommentDto>;
  toggleJoin(postId: string): Promise<ToggleResult>;
  participants(postId: string): Promise<readonly ParticipantDto[]>;
  toggleFollow(customerId: string): Promise<ToggleResult>;
}

export function createMemberService(client: ApiClient): MemberService {
  return {
    profile() {
      return client.get("/api/v1/member/me");
    },
    updateProfile(request) {
      return client.put("/api/v1/member/me", request) as Promise<MemberDto>;
    },
    async deleteAccount(password) {
      await client.request("DELETE", "/api/v1/member/me", { body: { password } satisfies MemberDeleteRequest });
    },

    async requestOtp(purpose) {
      await client.post("/api/v1/member/otp/request", { purpose });
    },
    verifyOtp(purpose, code) {
      return client.post(
        "/api/v1/member/otp/verify",
        { purpose, code },
      ) as Promise<MemberDto>;
    },

    appointments() {
      return client.get("/api/v1/member/appointments");
    },
    book(request) {
      return client.post("/api/v1/member/appointments", request) as Promise<BookAppointmentResponse>;
    },
    cancelAppointment(appointmentId) {
      return client.post(`/api/v1/member/appointments/${appointmentId}/cancel`).then(() => undefined);
    },
    packages() {
      return client.get("/api/v1/member/packages");
    },

    consents() {
      return client.get("/api/v1/member/consents");
    },
    submitConsents(entries) {
      const payload = { entries } as { entries: readonly ConsentEntry[] };
      return client.post("/api/v1/member/consents", payload) as Promise<
        readonly ConsentStateDto[]
      >;
    },

    cards() {
      return client.get("/api/v1/member/cards");
    },
    upsertCard(request) {
      return client.put("/api/v1/member/cards", request) as Promise<CardDto>;
    },

    myCode() {
      return client.get("/api/v1/member/code").then((res) => {
        const typed = res as unknown as MemberCodeResponse;
        if (typeof typed?.memberCode === "string") return typed.memberCode;
        throw new Error("memberCode missing");
      });
    },
    friends() {
      return client.get("/api/v1/member/friends");
    },
    addFriend(request) {
      return client.post("/api/v1/member/friends", request) as Promise<FriendDto>;
    },
    acceptFriend(friendshipId) {
      return client.post(`/api/v1/member/friends/${friendshipId}/accept`).then(() => undefined);
    },
    rejectFriend(friendshipId) {
      return client.post(`/api/v1/member/friends/${friendshipId}/reject`).then(() => undefined);
    },
    conversation(friendCustomerId, take) {
      return client.get(`/api/v1/member/messages/${friendCustomerId}`, { query: { take } });
    },
    sendMessage(friendCustomerId, request) {
      return client.post(
        `/api/v1/member/messages/${friendCustomerId}`,
        request,
      ) as Promise<MessageDto>;
    },

    feed() {
      return client.get("/api/v1/member/feed");
    },
    createPost(request) {
      return client.post("/api/v1/member/feed", request) as Promise<PostCreatedResponse>;
    },
    postMedia(postId) {
      return client
        .get<PostMediaDto>(`/api/v1/member/feed/${postId}/media`)
        .catch((error: unknown) => {
          if (
            error instanceof Error &&
            "code" in error &&
            (error as { code?: string }).code === "no_media"
          ) {
            return null;
          }
          throw error;
        });
    },
    deleteOwnPost(postId) {
      return client.post(`/api/v1/member/feed/${postId}/delete`).then(() => undefined);
    },
    toggleLike(postId) {
      return client.post(`/api/v1/member/feed/${postId}/like`) as Promise<ToggleResult>;
    },
    comments(postId) {
      return client.get(`/api/v1/member/feed/${postId}/comments`);
    },
    addComment(postId, request) {
      return client.post(`/api/v1/member/feed/${postId}/comments`, request) as Promise<CommentDto>;
    },
    toggleJoin(postId) {
      return client.post(`/api/v1/member/feed/${postId}/join`) as Promise<ToggleResult>;
    },
    participants(postId) {
      return client.get(`/api/v1/member/feed/${postId}/participants`);
    },
    toggleFollow(customerId) {
      return client.post(`/api/v1/member/follow/${customerId}`) as Promise<ToggleResult>;
    },
  };
}
