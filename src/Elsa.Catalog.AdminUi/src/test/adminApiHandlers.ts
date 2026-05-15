import { packageFixture, sourceFixture, syncRunFixture } from "@/test/fixtures";

export type MockResponse = {
  status: number;
  body?: unknown;
};

export function handleMockAdminRequest(path: string): MockResponse {
  if (path.endsWith("/api/admin/sources")) {
    return { status: 200, body: [sourceFixture] };
  }
  if (path.endsWith("/api/admin/packages")) {
    return { status: 200, body: [packageFixture] };
  }
  if (path.endsWith("/api/admin/sync-runs")) {
    return { status: 200, body: [syncRunFixture] };
  }
  return { status: 404, body: { title: "Not found" } };
}
