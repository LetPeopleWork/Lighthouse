import { useCallback, useContext, useEffect, useState } from "react";
import { useErrorSnackbar } from "../../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useArchiveConfirmationPreference } from "../../../../../hooks/useArchiveConfirmationPreference";
import type { Delivery } from "../../../../../models/Delivery";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import {
	DeliverySelectionMode,
	type IWorkItemRuleCondition,
} from "../../../../../models/WorkItemRules";
import { ApiError } from "../../../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";
import {
	archivedRefusalMessage,
	isArchivedRefusal,
	isNotArchivedRefusal,
	notArchivedRefusalMessage,
} from "../../../../../utils/deliveries/deliveryArchivedRefusal";

interface UseDeliveryManagementProps {
	portfolio: Portfolio;
}

// Deleting needs an id to act on and a name to put in the question, and nothing else - which is why
// a retired Delivery can go down the same path as a running one without pretending to be one.
export interface DeletableDelivery {
	id: number;
	name: string;
}

const CONCURRENCY_CONFLICT_STATUS = 409;

const STALE_VERSION_MESSAGE =
	"This was changed by someone else since you opened it. Refresh the page and try again.";

function isConcurrencyConflict(error: unknown): boolean {
	return (
		error instanceof ApiError && error.code === CONCURRENCY_CONFLICT_STATUS
	);
}

/**
 * Both refusals arrive as a conflict, and they call for opposite things: one says reload and try
 * again, the other says the Delivery is retired and no reload will change that. Telling somebody to
 * refresh a page that will say exactly the same thing is the failure this separates out.
 */
function refusalMessage(
	error: unknown,
	deliveryTerm: string,
	fallback: string,
): string {
	if (isArchivedRefusal(error)) {
		return archivedRefusalMessage(deliveryTerm);
	}

	if (isNotArchivedRefusal(error)) {
		return notArchivedRefusalMessage(deliveryTerm);
	}

	return isConcurrencyConflict(error) ? STALE_VERSION_MESSAGE : fallback;
}

export const useDeliveryManagement = ({
	portfolio,
}: UseDeliveryManagementProps) => {
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);

	const {
		shouldConfirm: shouldConfirmBeforeArchiving,
		stopAsking: stopAskingBeforeArchiving,
	} = useArchiveConfirmationPreference();

	const [deliveries, setDeliveries] = useState<Delivery[]>([]);
	const [archivedDeliveries, setArchivedDeliveries] = useState<
		ArchivedDelivery[]
	>([]);
	const [deliverySources, setDeliverySources] = useState<IDeliverySource[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [showCreateModal, setShowCreateModal] = useState(false);
	const [selectedDelivery, setSelectedDelivery] = useState<Delivery | null>(
		null,
	);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
	const [deliveryToDelete, setDeliveryToDelete] =
		useState<DeletableDelivery | null>(null);
	const [archiveDialogOpen, setArchiveDialogOpen] = useState(false);
	const [deliveryToArchive, setDeliveryToArchive] = useState<Delivery | null>(
		null,
	);
	const [expandedDeliveries, setExpandedDeliveries] = useState<Set<number>>(
		new Set(),
	);
	const [loadedFeatures, setLoadedFeatures] = useState<Map<number, IFeature[]>>(
		new Map(),
	);
	const [loadingFeaturesByDelivery, setLoadingFeaturesByDelivery] = useState<
		Set<number>
	>(new Set());

	const { deliveryService, featureService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();

	const loadFeaturesForDelivery = useCallback(
		async (delivery: Delivery): Promise<void> => {
			if (
				loadingFeaturesByDelivery.has(delivery.id) ||
				loadedFeatures.has(delivery.id)
			)
				return;

			if (!delivery.features || delivery.features.length === 0) {
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, []));
				return;
			}

			setLoadingFeaturesByDelivery((prev) => new Set(prev).add(delivery.id));

			try {
				const featureIds = delivery.features;
				const features = await featureService.getFeaturesByIds(featureIds);
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, features));
			} catch (error) {
				console.error("Failed to load features for delivery:", error);
				showError("Failed to load features for delivery");
			} finally {
				setLoadingFeaturesByDelivery((prev) => {
					const next = new Set(prev);
					next.delete(delivery.id);
					return next;
				});
			}
		},
		[featureService, showError, loadingFeaturesByDelivery, loadedFeatures],
	);

	const forceReloadFeaturesForDelivery = useCallback(
		async (delivery: Delivery): Promise<void> => {
			if (loadingFeaturesByDelivery.has(delivery.id)) return;

			if (!delivery.features || delivery.features.length === 0) {
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, []));
				return;
			}

			setLoadingFeaturesByDelivery((prev) => new Set(prev).add(delivery.id));

			try {
				const featureIds = delivery.features;
				const features = await featureService.getFeaturesByIds(featureIds);
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, features));
			} catch (error) {
				console.error("Failed to load features for delivery:", error);
				showError("Failed to load features for delivery");
			} finally {
				setLoadingFeaturesByDelivery((prev) => {
					const next = new Set(prev);
					next.delete(delivery.id);
					return next;
				});
			}
		},
		[featureService, showError, loadingFeaturesByDelivery],
	);

	// A Delivery that has left the live list takes its expansion and its loaded Features with it,
	// so nothing is left holding rows for something that is no longer there to show them.
	const forgetDelivery = useCallback((deliveryId: number) => {
		setExpandedDeliveries((prev) => {
			const next = new Set(prev);
			next.delete(deliveryId);
			return next;
		});
		setLoadedFeatures((prev) => {
			const next = new Map(prev);
			next.delete(deliveryId);
			return next;
		});
	}, []);

	const fetchDeliveries = useCallback(async () => {
		setIsLoading(true);
		try {
			const portfolioDeliveries = await deliveryService.getByPortfolio(
				portfolio.id,
			);
			setDeliveries(portfolioDeliveries.active);
			setArchivedDeliveries(portfolioDeliveries.archived);
		} catch (error) {
			console.error("Failed to fetch deliveries:", error);
			showError("Failed to fetch deliveries");
		} finally {
			setIsLoading(false);
		}
	}, [deliveryService, portfolio.id, showError]);

	// Asked once for the whole list rather than per row: it is what turns a stored source key into the
	// name a reader recognises, and every row on the page would otherwise ask the same question again.
	// A connection that cannot answer leaves the rows naming the key itself, which is worse to read but
	// still true, so a failure here is not worth interrupting anyone over.
	const fetchDeliverySources = useCallback(async () => {
		try {
			setDeliverySources(
				await deliveryService.getDeliverySources(portfolio.id),
			);
		} catch {
			setDeliverySources([]);
		}
	}, [deliveryService, portfolio.id]);

	const handleAddDelivery = () => {
		setShowCreateModal(true);
	};

	const handleDeleteDelivery = (delivery: DeletableDelivery) => {
		setDeliveryToDelete(delivery);
		setDeleteDialogOpen(true);
	};

	const handleEditDelivery = (delivery: Delivery) => {
		setSelectedDelivery(delivery);
	};

	const handleArchiveDelivery = async (delivery: Delivery) => {
		// Somebody who has said they do not want asking again gets the action, not the question.
		if (!shouldConfirmBeforeArchiving) {
			await archiveDelivery(delivery);
			return;
		}

		setDeliveryToArchive(delivery);
		setArchiveDialogOpen(true);
	};

	const handleCreateDelivery = async (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IWorkItemRuleCondition[];
		mode?: "and" | "or";
		sourceKey?: string;
		sourceReference?: string;
	}) => {
		try {
			await deliveryService.create({
				portfolioId: portfolio.id,
				name: deliveryData.name,
				date: new Date(deliveryData.date),
				featureIds: deliveryData.featureIds,
				selectionMode: deliveryData.selectionMode,
				rules: deliveryData.rules,
				mode: deliveryData.mode,
				sourceKey: deliveryData.sourceKey,
				sourceReference: deliveryData.sourceReference,
			});
			setShowCreateModal(false);
			await fetchDeliveries();
		} catch (error) {
			console.error("Failed to create delivery:", error);
			showError("Failed to create delivery");
		}
	};

	const handleUpdateDelivery = async (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: DeliverySelectionMode;
		rules?: IWorkItemRuleCondition[];
		mode?: "and" | "or";
		sourceKey?: string;
		sourceReference?: string;
		concurrencyToken?: string;
	}) => {
		try {
			const wasExpanded = expandedDeliveries.has(deliveryData.id);

			await deliveryService.update({
				deliveryId: deliveryData.id,
				name: deliveryData.name,
				date: new Date(deliveryData.date),
				featureIds: deliveryData.featureIds,
				selectionMode: deliveryData.selectionMode,
				rules: deliveryData.rules,
				mode: deliveryData.mode,
				sourceKey: deliveryData.sourceKey,
				sourceReference: deliveryData.sourceReference,
				concurrencyToken: deliveryData.concurrencyToken,
			});
			setSelectedDelivery(null);
			setLoadedFeatures((prev) => {
				const next = new Map(prev);
				next.delete(deliveryData.id);
				return next;
			});
			await fetchDeliveries();

			if (wasExpanded) {
				const updatedDeliveries = await deliveryService.getByPortfolio(
					portfolio.id,
				);
				const updatedDelivery = updatedDeliveries.active.find(
					(d) => d.id === deliveryData.id,
				);
				if (updatedDelivery) {
					await forceReloadFeaturesForDelivery(updatedDelivery);
				}
			}
		} catch (error) {
			console.error("Failed to update delivery:", error);
			showError(
				refusalMessage(error, deliveryTerm, `Failed to update ${deliveryTerm}`),
			);
		}
	};

	/**
	 * The server ignores everything but the mode here: a delivery let go of its source keeps the name,
	 * the date and the work the source last gave it. They are sent back unchanged anyway, so nothing
	 * about this call reads as an edit, and from here on they are the reader's to change.
	 */
	const handleUnbindDelivery = async (delivery: Delivery) => {
		try {
			await deliveryService.update({
				deliveryId: delivery.id,
				name: delivery.name,
				date: new Date(delivery.date),
				featureIds: delivery.features,
				selectionMode: DeliverySelectionMode.Manual,
				concurrencyToken: delivery.concurrencyToken,
			});
			setLoadedFeatures((prev) => {
				const next = new Map(prev);
				next.delete(delivery.id);
				return next;
			});
			await fetchDeliveries();
		} catch (error) {
			console.error(
				`Failed to release ${deliveryTerm} from its source:`,
				error,
			);
			showError(
				refusalMessage(
					error,
					deliveryTerm,
					`Failed to release this ${deliveryTerm} from its source`,
				),
			);
		}
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && deliveryToDelete) {
			try {
				await deliveryService.delete(deliveryToDelete.id);
				forgetDelivery(deliveryToDelete.id);
				await fetchDeliveries();
			} catch (error) {
				console.error("Failed to delete delivery:", error);
				showError("Failed to delete delivery");
			}
		}

		setDeleteDialogOpen(false);
		setDeliveryToDelete(null);
	};

	const archiveDelivery = async (delivery: Delivery) => {
		try {
			await deliveryService.archive(delivery.id, delivery.concurrencyToken);
			forgetDelivery(delivery.id);
			await fetchDeliveries();
		} catch (error) {
			console.error("Failed to archive delivery:", error);
			showError(
				refusalMessage(
					error,
					deliveryTerm,
					`Failed to archive ${deliveryTerm}`,
				),
			);
		}
	};

	const handleArchiveConfirmation = async (
		confirmed: boolean,
		stopAsking = false,
	) => {
		if (confirmed && deliveryToArchive) {
			if (stopAsking) {
				stopAskingBeforeArchiving();
			}

			try {
				await deliveryService.archive(
					deliveryToArchive.id,
					deliveryToArchive.concurrencyToken,
				);
				forgetDelivery(deliveryToArchive.id);
				await fetchDeliveries();
			} catch (error) {
				console.error("Failed to archive delivery:", error);
				showError(
					refusalMessage(
						error,
						deliveryTerm,
						`Failed to archive ${deliveryTerm}`,
					),
				);
			}
		}

		setArchiveDialogOpen(false);
		setDeliveryToArchive(null);
	};

	const handleUnarchiveDelivery = async (delivery: ArchivedDelivery) => {
		try {
			await deliveryService.unarchive(delivery.id, delivery.concurrencyToken);
			await fetchDeliveries();
		} catch (error) {
			console.error("Failed to unarchive delivery:", error);
			showError(
				refusalMessage(
					error,
					deliveryTerm,
					`Failed to bring back ${deliveryTerm}`,
				),
			);
		}
	};

	const handleCloseCreateModal = () => {
		setShowCreateModal(false);
	};

	const handleCloseEditModal = () => {
		setSelectedDelivery(null);
	};

	const handleToggleExpanded = useCallback(
		(deliveryId: number) => {
			const isCurrentlyExpanded = expandedDeliveries.has(deliveryId);

			setExpandedDeliveries((prev) => {
				const next = new Set(prev);
				if (isCurrentlyExpanded) {
					next.delete(deliveryId);
				} else {
					next.add(deliveryId);
					const delivery = deliveries.find((d) => d.id === deliveryId);
					if (delivery) {
						loadFeaturesForDelivery(delivery);
					}
				}
				return next;
			});
		},
		[expandedDeliveries, deliveries, loadFeaturesForDelivery],
	);

	useEffect(() => {
		fetchDeliveries();
	}, [fetchDeliveries]);

	useEffect(() => {
		fetchDeliverySources();
	}, [fetchDeliverySources]);

	return {
		deliveries,
		archivedDeliveries,
		deliverySources,
		isLoading,
		showCreateModal,
		selectedDelivery,
		deleteDialogOpen,
		deliveryToDelete,
		archiveDialogOpen,
		deliveryToArchive,
		expandedDeliveries,
		loadedFeatures,
		loadingFeaturesByDelivery,

		handleAddDelivery,
		handleDeleteDelivery,
		handleEditDelivery,
		handleArchiveDelivery,
		handleArchiveConfirmation,
		handleUnarchiveDelivery,
		handleDeleteConfirmation,
		handleCloseCreateModal,
		handleCloseEditModal,
		handleCreateDelivery,
		handleUpdateDelivery,
		handleUnbindDelivery,
		handleToggleExpanded,
	};
};

export default useDeliveryManagement;
