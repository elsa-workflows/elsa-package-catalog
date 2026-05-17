import { packageDetailsFixture, packageFixture, packageWithoutVersionsFixture, sourceFixture, syncRunFixture, validationFindingsFixture } from "@/test/fixtures";

export type MockResponse = {
  status: number;
  body?: unknown;
};

export function handleMockAdminRequest(path: string, method = "GET"): MockResponse {
  if (path.endsWith("/api/admin/sources")) {
    return { status: 200, body: [sourceFixture] };
  }
  if (path.endsWith("/api/admin/packages")) {
    return { status: 200, body: [packageFixture] };
  }
  if (path.includes("/api/admin/packages/") && path.includes("/versions/") && path.endsWith("/validation")) {
    return { status: 200, body: validationFindingsFixture };
  }
  if (path.includes("/api/admin/packages/") && path.includes("/versions/") && path.endsWith("/manifest")) {
    return { status: 200, body: packageDetailsFixture.versions[0].manifest };
  }
  if (path.includes("/api/admin/packages/") && path.includes("/versions/") && (path.endsWith("/approve") || path.endsWith("/reject")) && method === "POST") {
    return { status: 204 };
  }
  const normalizedPath = path.toLowerCase();
  if (normalizedPath.endsWith(`/api/admin/packages/${encodeURIComponent(packageDetailsFixture.packageId).toLowerCase()}`)) {
    return { status: 200, body: packageDetailsFixture };
  }
  if (normalizedPath.endsWith(`/api/admin/packages/${encodeURIComponent(packageWithoutVersionsFixture.packageId).toLowerCase()}`)) {
    return { status: 200, body: packageWithoutVersionsFixture };
  }
  if (path.endsWith("/api/admin/sync-runs")) {
    return { status: 200, body: [syncRunFixture] };
  }
  return { status: 404, body: { title: "Not found" } };
}
