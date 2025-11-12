import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
	useColumnOrder,
	useColumnVisibility,
	usePersistedGridState,
} from "./hooks";

// Mock localStorage
const localStorageMock = (() => {
	let store: Record<string, string> = {};

	return {
		getItem: (key: string) => store[key] || null,
		setItem: (key: string, value: string) => {
			store[key] = value;
		},
		removeItem: (key: string) => {
			delete store[key];
		},
		clear: () => {
			store = {};
		},
	};
})();

Object.defineProperty(globalThis, "localStorage", {
	value: localStorageMock,
	writable: true,
});

beforeEach(() => {
	localStorageMock.clear();
	vi.clearAllMocks();
});

describe("useColumnVisibility", () => {
	it("should initialize with empty hidden columns by default", () => {
		const { result } = renderHook(() => useColumnVisibility());

		expect(result.current.hiddenColumns).toEqual([]);
	});

	it("should initialize with provided initial hidden columns", () => {
		const initialHidden = ["age", "email"];
		const { result } = renderHook(() => useColumnVisibility(initialHidden));

		expect(result.current.hiddenColumns).toEqual(initialHidden);
	});

	it("should toggle column visibility", () => {
		const { result } = renderHook(() => useColumnVisibility());

		act(() => {
			result.current.toggleColumn("age");
		});

		expect(result.current.hiddenColumns).toContain("age");

		act(() => {
			result.current.toggleColumn("age");
		});

		expect(result.current.hiddenColumns).not.toContain("age");
	});

	it("should hide a column", () => {
		const { result } = renderHook(() => useColumnVisibility());

		act(() => {
			result.current.hideColumn("email");
		});

		expect(result.current.hiddenColumns).toContain("email");

		// Hiding again should not duplicate
		act(() => {
			result.current.hideColumn("email");
		});

		expect(
			result.current.hiddenColumns.filter((c: string) => c === "email").length,
		).toBe(1);
	});

	it("should show a column", () => {
		const { result } = renderHook(() => useColumnVisibility(["age", "email"]));

		act(() => {
			result.current.showColumn("age");
		});

		expect(result.current.hiddenColumns).not.toContain("age");
		expect(result.current.hiddenColumns).toContain("email");
	});

	it("should reset to initial state", () => {
		const initialHidden = ["age"];
		const { result } = renderHook(() => useColumnVisibility(initialHidden));

		act(() => {
			result.current.hideColumn("email");
			result.current.showColumn("age");
		});

		expect(result.current.hiddenColumns).toEqual(["email"]);

		act(() => {
			result.current.reset();
		});

		expect(result.current.hiddenColumns).toEqual(initialHidden);
	});

	it("should check if column is hidden", () => {
		const { result } = renderHook(() => useColumnVisibility(["age"]));

		expect(result.current.isHidden("age")).toBe(true);
		expect(result.current.isHidden("name")).toBe(false);
	});
});

describe("useColumnOrder", () => {
	it("should initialize with provided column order", () => {
		const initialOrder = ["id", "name", "age", "email"];
		const { result } = renderHook(() => useColumnOrder(initialOrder));

		expect(result.current.columnOrder).toEqual(initialOrder);
	});

	it("should reorder columns", () => {
		const initialOrder = ["id", "name", "age", "email"];
		const { result } = renderHook(() => useColumnOrder(initialOrder));

		act(() => {
			result.current.moveColumn(0, 2); // Move "id" to index 2
		});

		expect(result.current.columnOrder).toEqual(["name", "age", "id", "email"]);
	});

	it("should reset to initial order", () => {
		const initialOrder = ["id", "name", "age", "email"];
		const { result } = renderHook(() => useColumnOrder(initialOrder));

		act(() => {
			result.current.moveColumn(0, 2);
		});

		expect(result.current.columnOrder).not.toEqual(initialOrder);

		act(() => {
			result.current.reset();
		});

		expect(result.current.columnOrder).toEqual(initialOrder);
	});
});

describe("usePersistedGridState", () => {
	const storageKey = "test-grid";

	it("should initialize with empty state when no persisted data exists", () => {
		const { result } = renderHook(() => usePersistedGridState(storageKey));

		expect(result.current.state).toEqual({
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
		});
	});

	it("should save state to localStorage", () => {
		const { result } = renderHook(() => usePersistedGridState(storageKey));

		const newState = {
			sortModel: [{ field: "name", sort: "asc" as const }],
			columnVisibilityModel: { age: false },
			columnOrder: ["id", "name", "email"],
		};

		act(() => {
			result.current.saveState(newState);
		});

		expect(result.current.state).toEqual(newState);

		// Verify it's in localStorage
		const stored = JSON.parse(localStorageMock.getItem(storageKey) || "{}");
		expect(stored).toEqual(newState);
	});

	it("should load state from localStorage on mount", () => {
		const existingState = {
			sortModel: [{ field: "age", sort: "desc" as const }],
			columnVisibilityModel: { email: false },
			columnOrder: ["name", "id", "age", "email"],
		};

		localStorageMock.setItem(storageKey, JSON.stringify(existingState));

		const { result } = renderHook(() => usePersistedGridState(storageKey));

		expect(result.current.state).toEqual(existingState);
	});

	it("should clear state from localStorage", () => {
		const { result } = renderHook(() => usePersistedGridState(storageKey));

		const newState = {
			sortModel: [{ field: "name", sort: "asc" as const }],
		};

		act(() => {
			result.current.saveState(newState);
		});

		expect(result.current.state).toEqual(newState);

		act(() => {
			result.current.clearState();
		});

		expect(result.current.state).toEqual({
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
		});
		expect(localStorageMock.getItem(storageKey)).toBeNull();
	});

	it("should handle invalid JSON in localStorage gracefully", () => {
		localStorageMock.setItem(storageKey, "invalid json{");

		const { result } = renderHook(() => usePersistedGridState(storageKey));

		// Should initialize with empty state despite invalid JSON
		expect(result.current.state).toEqual({
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
		});
	});
});
