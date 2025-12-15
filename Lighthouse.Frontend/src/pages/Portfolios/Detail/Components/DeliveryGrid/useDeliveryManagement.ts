import { useCallback, useContext, useEffect, useState } from "react";
import { useErrorSnackbar } from "../../../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { Delivery } from "../../../../../models/Delivery";
import type { IFeature } from "../../../../../models/Feature";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";

interface UseDeliveryManagementProps {
	portfolio: Portfolio;
}

export const useDeliveryManagement = ({
	portfolio,
}: UseDeliveryManagementProps) => {
	const [deliveries, setDeliveries] = useState<Delivery[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [showCreateModal, setShowCreateModal] = useState(false);
	const [selectedDelivery, setSelectedDelivery] = useState<Delivery | null>(
		null,
	);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
	const [deliveryToDelete, setDeliveryToDelete] = useState<Delivery | null>(
		null,
	);
	// New state for expansion and feature loading
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
		async (delivery: Delivery) => {
			// Skip if features are already loaded
			if (loadedFeatures.has(delivery.id)) {
				return;
			}

			// Skip if no feature IDs
			if (!delivery.features || delivery.features.length === 0) {
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, []));
				return;
			}

			setLoadingFeaturesByDelivery((prev) => new Set(prev).add(delivery.id));

			try {
				const featureIds = delivery.features.map((f) => f.id);
				const features = await featureService.getFeaturesByIds(featureIds);
				setLoadedFeatures((prev) => new Map(prev).set(delivery.id, features));
			} catch (_error) {
				showError("Failed to load features for delivery");
			} finally {
				setLoadingFeaturesByDelivery((prev) => {
					const next = new Set(prev);
					next.delete(delivery.id);
					return next;
				});
			}
		},
		[featureService, loadedFeatures, showError],
	);

	const fetchDeliveries = useCallback(async () => {
		setIsLoading(true);
		try {
			const portfolioDeliveries = await deliveryService.getByPortfolio(
				portfolio.id,
			);
			setDeliveries(portfolioDeliveries);
		} catch (_error) {
			showError("Failed to fetch deliveries");
		} finally {
			setIsLoading(false);
		}
	}, [deliveryService, portfolio.id, showError]);

	const handleAddDelivery = () => {
		setShowCreateModal(true);
	};

	const handleDeleteDelivery = (delivery: Delivery) => {
		setDeliveryToDelete(delivery);
		setDeleteDialogOpen(true);
	};

	const handleEditDelivery = (delivery: Delivery) => {
		setSelectedDelivery(delivery);
	};

	const onDeliveryUpdate = () => {
		fetchDeliveries();
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && deliveryToDelete) {
			try {
				await deliveryService.delete(deliveryToDelete.id); // Clean up expansion state and loaded features
				setExpandedDeliveries((prev) => {
					const next = new Set(prev);
					next.delete(deliveryToDelete.id);
					return next;
				});
				setLoadedFeatures((prev) => {
					const next = new Map(prev);
					next.delete(deliveryToDelete.id);
					return next;
				});
				await fetchDeliveries();
			} catch (_error) {
				showError("Failed to delete delivery");
			}
		}

		setDeleteDialogOpen(false);
		setDeliveryToDelete(null);
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
					// Load features when expanding
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

	return {
		// State
		deliveries,
		isLoading,
		showCreateModal,
		selectedDelivery,
		deleteDialogOpen,
		deliveryToDelete,
		// New expansion state
		expandedDeliveries,
		loadedFeatures,
		loadingFeaturesByDelivery,

		// Actions
		handleAddDelivery,
		handleDeleteDelivery,
		handleEditDelivery,
		handleDeleteConfirmation,
		handleCloseCreateModal,
		handleCloseEditModal,
		onDeliveryUpdate,
		// New expansion action
		handleToggleExpanded,
	};
};

export default useDeliveryManagement;
