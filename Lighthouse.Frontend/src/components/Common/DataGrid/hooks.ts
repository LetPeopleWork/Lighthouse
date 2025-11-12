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
	};
}

/**
 * Hook for persisting grid state to localStorage
 * @param storageKey - Unique key for localStorage
 * @returns Object with persisted state and control functions
 */
export function usePersistedGridState(storageKey: string) {
	const [state, setState] = useState<PersistedGridState>(() => {
		try {
			const stored = localStorage.getItem(storageKey);
			if (stored) {
				return JSON.parse(stored);
			}
		} catch (error) {
			console.error("Error loading persisted grid state:", error);
		}
		return {
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
		};
	});

	const saveState = useCallback(
		(newState: PersistedGridState) => {
			setState(newState);
			try {
				localStorage.setItem(storageKey, JSON.stringify(newState));
			} catch (error) {
				console.error("Error saving grid state to localStorage:", error);
			}
		},
		[storageKey],
	);

	const clearState = useCallback(() => {
		setState({
			sortModel: undefined,
			columnVisibilityModel: undefined,
			columnOrder: undefined,
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
		clearState,
	};
}
