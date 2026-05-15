import { describe, expect, it } from "vitest";
import { hasSuspiciousChange, type CatalogPackage } from "@/features/packages/packageModels";

const packageItem: CatalogPackage = {
  packageId: "Elsa.Test",
  approved: true,
  listed: true,
  latestVersion: "2.0.0",
  versions: [
    {
      version: "1.0.0",
      approvalStatus: "Approved",
      validationStatus: "Valid",
      isListed: true,
      suspiciousChangeDetected: true,
      schemaVersion: "1.0"
    },
    {
      version: "2.0.0",
      approvalStatus: "Approved",
      validationStatus: "Valid",
      isListed: true,
      suspiciousChangeDetected: false,
      schemaVersion: "1.0"
    }
  ]
};

describe("packageModels", () => {
  it("reports suspicious changes for the latest version only", () => {
    expect(hasSuspiciousChange(packageItem)).toBe(false);
    expect(hasSuspiciousChange({ ...packageItem, latestVersion: "1.0.0" })).toBe(true);
  });
});
