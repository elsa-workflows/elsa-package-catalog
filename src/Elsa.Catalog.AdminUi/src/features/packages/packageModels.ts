export type PackageApprovalStatus = "Pending" | "Approved" | "Rejected";
export type ValidationStatus = "NotValidated" | "Valid" | "Invalid" | "UnsupportedSchema";

export type PackageVersionSummary = {
  version: string;
  validationStatus: ValidationStatus;
  approvalStatus: PackageApprovalStatus;
  isListed: boolean;
  suspiciousChangeDetected: boolean;
  schemaVersion?: string | null;
};

export type CatalogPackage = {
  packageId: string;
  approved: boolean;
  listed: boolean;
  latestVersion?: string | null;
  versions: PackageVersionSummary[];
  sourceId?: string | null;
  approvalStatus?: PackageApprovalStatus;
  validationStatus?: ValidationStatus;
  featuresCount?: number | null;
  updatedAt?: string | null;
};

export type PackageFilter = "All" | "Pending" | "Approved" | "Rejected" | "Invalid" | "Suspicious" | "Unlisted";
export type PackageSort = "packageId" | "latestVersion" | "approvalStatus" | "validationStatus" | "updatedAt";

export type SelectablePackageVersion = {
  packageId: string;
  version: string;
};

const approvalOrder: PackageApprovalStatus[] = ["Pending", "Rejected", "Approved"];
const validationOrder: ValidationStatus[] = ["Invalid", "UnsupportedSchema", "NotValidated", "Valid"];

export function latestVersion(packageItem: CatalogPackage) {
  return packageItem.latestVersion ?? packageItem.versions[0]?.version ?? null;
}

export function latestVersionSummary(packageItem: CatalogPackage) {
  const latest = latestVersion(packageItem);
  return packageItem.versions.find((version) => version.version === latest) ?? packageItem.versions[0] ?? null;
}

export function packageApprovalStatus(packageItem: CatalogPackage): PackageApprovalStatus {
  if (packageItem.approvalStatus) return packageItem.approvalStatus;
  const statuses = packageItem.versions.map((version) => version.approvalStatus);
  return approvalOrder.find((status) => statuses.includes(status)) ?? (packageItem.approved ? "Approved" : "Pending");
}

export function packageValidationStatus(packageItem: CatalogPackage): ValidationStatus {
  if (packageItem.validationStatus) return packageItem.validationStatus;
  const statuses = packageItem.versions.map((version) => version.validationStatus);
  return validationOrder.find((status) => statuses.includes(status)) ?? "NotValidated";
}

export function isPackageListed(packageItem: CatalogPackage) {
  return packageItem.listed && (latestVersionSummary(packageItem)?.isListed ?? true);
}

export function hasSuspiciousChange(packageItem: CatalogPackage) {
  return packageItem.versions.some((version) => version.suspiciousChangeDetected);
}

export function selectableLatestVersion(packageItem: CatalogPackage): SelectablePackageVersion | null {
  const latest = latestVersionSummary(packageItem);
  return latest ? { packageId: packageItem.packageId, version: latest.version } : null;
}

export function selectionKey(item: SelectablePackageVersion) {
  return `${item.packageId}@${item.version}`;
}
