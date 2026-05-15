import { apiRequest } from "@/lib/api/httpClient";
import type { CatalogPackage, SelectablePackageVersion } from "@/features/packages/packageModels";

export function listPackages() {
  return apiRequest<CatalogPackage[]>("/api/admin/packages");
}

export function approvePackageVersion(item: SelectablePackageVersion, reason?: string) {
  return apiRequest<void>(`/api/admin/packages/${encodeURIComponent(item.packageId)}/versions/${encodeURIComponent(item.version)}/approve`, {
    method: "POST",
    body: JSON.stringify({ reason: reason?.trim() || null })
  });
}

export function rejectPackageVersion(item: SelectablePackageVersion, reason: string) {
  return apiRequest<void>(`/api/admin/packages/${encodeURIComponent(item.packageId)}/versions/${encodeURIComponent(item.version)}/reject`, {
    method: "POST",
    body: JSON.stringify({ reason: reason.trim() })
  });
}
