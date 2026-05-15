import { describe, expect, it } from "vitest";
import { sourceStatusTone } from "@/lib/status/statusBadges";

describe("sourceStatusTone", () => {
  it("treats unsupported schemas as destructive validation states", () => {
    expect(sourceStatusTone("UnsupportedSchema")).toBe("destructive");
  });
});
