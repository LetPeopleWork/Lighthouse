import { useCallback, useState } from "react";
import type { PersistedGridState } from "./types";

/**
 * Hook for managing column visibility state
 * @param initialHiddenColumns - Array of initially hidden column field names
 * @returns Object with visibility state and control functions
 */
export function useColumnVisibility(initialHiddenColumns: string[] = []) {
	const [hiddenColumns, setHiddenColumns] =
		useState<string[]>(initialHiddenColumns);

	const toggleColumn = useCallback((field: string) => {
		setHiddenColumns((prev) =>
			prev.includes(field) ? prev.filter((f) => f !== field) : [...prev, field],
		);
	}, []);

	const hideColumn = useCallback((field: string) => {
		setHiddenColumns((prev) =>
			prev.includes(field) ? prev : [...prev, field],
		);
	}, []);

	const showColumn = useCallback((field: string) => {
		setHiddenColumns((prev) => prev.filter((f) => f !== field));
	}, []);

	const reset = useCallback(() => {
		setHiddenColumns(initialHiddenColumns);
	}, [initialHiddenColumns]);

	const isHidden = useCallback(
		(field: string) => hiddenColumns.includes(field),
		[hiddenColumns],
	);

	return {
		hiddenColumns,
		toggleColumn,
		hideColumn,
		showColumn,
		reset,
		isHidden,
	};
}

/**
 * Hook for managing column order state
 * @param initialColumnOrder - Array of initial column field names in order
 * @returns Object with column order state and control functions
 */
export function useColumnOrder(initialColumnOrder: string[]) {
	const [columnOrder, setColumnOrder] = useState<string[]>(initialColumnOrder);

	const moveColumn = useCallback((fromIndex: number, toIndex: number) => {
		setColumnOrder((prev) => {
			const newOrder = [...prev];
			const [movedItem] = newOrder.splice(fromIndex, 1);
			newOrder.splice(toIndex, 0, movedItem);
			return newOrder;
		});
	}, []);

	const reset = useCallback(() => {
		setColumnOrder(initialColumnOrder);
	}, [initialColumnOrder]);

	return {
		columnOrder,
		moveColumn,
		reset,
		setColumnOrder,
	};
}

/**
 * Sanitizes the grid state to prevent storage poisoning.
 * Ensures only expected properties are saved and values are typed correctly.
 */
function sanitizeGridState(dirtyState: PersistedGridState): PersistedGridState {
	const cleanState: PersistedGridState = {
		sortModel: undefined,
		columnVisibilityModel: undefined,
		columnOrder: undefined,
		columnWidths: undefined,
	};

	if (!dirtyState || typeof dirtyState !== "object") return cleanState;

	// 1. Sanitize Column Order (Array of strings)
	if (Array.isArray(dirtyState.columnOrder)) {
		cleanState.columnOrder = dirtyState.columnOrder
			.filter((item): item is string => typeof item === "string")
			// Remove potential script tags or excessive lengths
			.map((s) => s.replaceAll(/[<>]/g, "").substring(0, 100));
	}

	// 2. Sanitize Column Widths (Object with numeric values)
	if (dirtyState.columnWidths && typeof dirtyState.columnWidths === "object") {
		const widths: Record<string, number> = {};
		for (const [key, value] of Object.entries(dirtyState.columnWidths)) {
			if (typeof value === "number") {
				widths[key.replaceAll(/[<>]/g, "").substring(0, 100)] = value;
			}
		}
		cleanState.columnWidths = widths;
	}

	// 3. Sanitize Visibility Model (Object with boolean values)
	if (
		dirtyState.columnVisibilityModel &&
		typeof dirtyState.columnVisibilityModel === "object"
	) {
		const visibility: Record<string, boolean> = {};
		for (const [key, value] of Object.entries(
			dirtyState.columnVisibilityModel,
		)) {
			visibility[key.replaceAll(/[<>]/g, "").substring(0, 100)] =
				Boolean(value);
		}
		cleanState.columnVisibilityModel = visibility;
	}

	// 4. Sanitize Sort Model (Array of objects)
	if (Array.isArray(dirtyState.sortModel)) {
		cleanState.sortModel = dirtyState.sortModel.filter(
			(s) =>
				s &&
				typeof s.field === "string" &&
				(s.sort === "asc" || s.sort === "desc" || s.sort === null),
		);
	}

	return cleanState;
}

/**
 * Hook for persisting grid state to localStorage
 * @param storageKey - Unique key for localStorage
 * @returns Object with persisted state and control functions
 */
export function usePersistedGridState(storageKey: string) {
	// 1. Sanitize on Initial Load
	const [state, setState] = useState<PersistedGridState>(() => {
		try {
			const stored = localStorage.getItem(storageKey);
			if (stored) {
				const parsed = JSON.parse(stored);
				return sanitizeGridState(parsed);
			}
		} catch (error) {
			console.error("Error loading persisted grid state:", error);
		}
		return {
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
			columnWidths: undefined,
		};
	});

	const saveState = useCallback(
		(newState: PersistedGridState) => {
			setState((prev) => {
				const merged = { ...prev, ...newState };
				const sanitized = sanitizeGridState(merged);
				try {
					localStorage.setItem(storageKey, JSON.stringify(sanitized));
				} catch (error) {
					console.error("Error saving grid state:", error);
				}
				return sanitized;
			});
		},
		[storageKey],
	);

	const updateState = useCallback(
		(updater: (prev: PersistedGridState) => PersistedGridState) => {
			setState((prev) => {
				const next = updater(prev);
				const merged = { ...prev, ...next };
				const sanitized = sanitizeGridState(merged);
				try {
					localStorage.setItem(storageKey, JSON.stringify(sanitized));
				} catch (error) {
					console.error("Error saving grid state:", error);
				}
				return sanitized;
			});
		},
		[storageKey],
	);

	const clearState = useCallback(() => {
		setState({
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
			columnWidths: undefined,
		});
		try {
			localStorage.removeItem(storageKey);
		} catch (error) {
			console.error("Error clearing grid state from localStorage:", error);
		}
	}, [storageKey]);

	return {
		state,
		saveState,
		updateState,
		clearState,
	};
}

/**
 * Hook for managing column widths.
 * @param initialWidths - Object mapping column field names to widths (pixels)
 */
export function useColumnWidths(
	initialWidths: { [field: string]: number } = {},
) {
	const [columnWidths, setColumnWidths] = useState<{ [field: string]: number }>(
		initialWidths,
	);

	const setColumnWidth = useCallback((field: string, width: number) => {
		setColumnWidths((prev) => ({ ...prev, [field]: width }));
	}, []);

	const setAllWidths = useCallback(
		(nextWidths: { [field: string]: number }) => {
			setColumnWidths(nextWidths);
		},
		[],
	);

	const reset = useCallback(() => {
		setColumnWidths(initialWidths);
	}, [initialWidths]);

	return {
		columnWidths,
		setColumnWidth,
		setAllWidths,
		reset,
	};
}
