import { act } from "@testing-library/react";
import { vi } from "vitest";
import { DEBOUNCE_MS } from "../hooks/useModifySettings";

const MARGIN_MS = 200;

/**
 * Moves the fake clock on rather than waiting it out, so a pending autosave has surely fired by
 * the time nothing is checked as saved, however slowly the machine is going.
 */
export const pastTheAutosaveDelay = () =>
	act(async () => {
		await vi.advanceTimersByTimeAsync(DEBOUNCE_MS + MARGIN_MS);
	});
