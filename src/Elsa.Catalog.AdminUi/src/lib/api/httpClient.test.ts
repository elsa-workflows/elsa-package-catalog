import { afterEach, describe, expect, it, vi } from "vitest";
import { apiRequest } from "@/lib/api/httpClient";

describe("apiRequest", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("surfaces API validation error arrays", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify({
      errors: ["Polling interval must be an ISO 8601 duration, for example PT30M."]
    }), {
      status: 400,
      statusText: "Bad Request",
      headers: { "Content-Type": "application/json" }
    })));

    await expect(apiRequest("/api/admin/sources")).rejects.toMatchObject({
      kind: "Validation",
      message: "Polling interval must be an ISO 8601 duration, for example PT30M."
    });
  });
});
