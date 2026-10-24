import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import type { PremiumVerdict } from "./premiumVerdict";
import {
	type DeliverySourceOptionsState,
	useDeliverySourceOptions,
} from "./useDeliverySourceOptions";
import {
	type DeliverySourcesQuery,
	useDeliverySources,
} from "./useDeliverySources";

/** Whether the server has said which ways to pick this Portfolio's connection offers. */
export type SourcesStatus = "loading" | "error" | "ready";

const sourcesStatusOf: Record<DeliverySourcesQuery["status"], SourcesStatus> = {
	pending: "loading",
	error: "error",
	success: "ready",
};

/**
 * The ways to pick are the page's answer, read here rather than asked for again. A failed answer is
 * the one exception: the notice tells the reader to reopen the dialog to retry, so each opening that
 * finds it failed asks once more, and only once, however often the answer fails again.
 */
const useSourcesAskedAgainAfterAFailure = (
	sources: DeliverySourcesQuery,
	open: boolean,
	openingNumber: number,
): void => {
	const handledOpening = useRef<number | null>(null);
	const { status, refetch } = sources;

	useEffect(() => {
		if (!open || handledOpening.current === openingNumber) {
			return;
		}
		handledOpening.current = openingNumber;
		if (status === "error") {
			refetch().catch(() => undefined);
		}
	}, [open, openingNumber, status, refetch]);
};

interface DialogDeliverySources {
	sources: IDeliverySource[];
	sourcesStatus: SourcesStatus;
	sourceOptionsOf: (sourceKey: string) => DeliverySourceOptionsState;
	/** Which opening of the dialog this is, so its other answers are asked for once per opening too. */
	opening: number;
	/** Called as the dialog closes, so its next opening asks the tracker afresh. */
	startNewOpening: () => void;
}

/** The ways to pick a Portfolio's connection offers, and the entries each one holds, per opening. */
export const useDialogDeliverySources = (
	portfolioId: number,
	open: boolean,
	verdict: PremiumVerdict,
): DialogDeliverySources => {
	const sourcesQuery = useDeliverySources(portfolioId);
	const sources = useMemo(() => sourcesQuery.data ?? [], [sourcesQuery.data]);
	const [openingNumber, setOpeningNumber] = useState(0);
	useSourcesAskedAgainAfterAFailure(sourcesQuery, open, openingNumber);
	const sourceOptionsOf = useDeliverySourceOptions({
		portfolioId,
		isOpen: open,
		opening: openingNumber,
		offeredSources: sourcesQuery.data,
		verdict,
	});
	const startNewOpening = useCallback(() => {
		setOpeningNumber((previous) => previous + 1);
	}, []);

	return {
		sources,
		sourcesStatus: sourcesStatusOf[sourcesQuery.status],
		sourceOptionsOf,
		opening: openingNumber,
		startNewOpening,
	};
};
