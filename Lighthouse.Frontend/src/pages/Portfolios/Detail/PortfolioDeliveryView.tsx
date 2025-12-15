import { Box } from "@mui/material";
import type React from "react";
import type { Portfolio } from "../../../models/Portfolio/Portfolio";
import {
	DeliveryHeader,
	DeliveryModals,
	useDeliveryManagement,
} from "./Components/DeliveryGrid";
import DeliverySection from "./Components/DeliveryGrid/DeliverySection";

interface PortfolioDeliveryViewProps {
	project: Portfolio;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	project,
}) => {
	const {
		deliveries,
		showCreateModal,
		selectedDelivery,
		deleteDialogOpen,
		deliveryToDelete,
		handleAddDelivery,
		handleDeleteDelivery,
		handleDeleteConfirmation,
		handleCloseCreateModal,
		handleCloseEditModal,
		onDeliveryUpdate,
		// New expansion state
		expandedDeliveries,
		loadedFeatures,
		loadingFeaturesByDelivery,
		handleToggleExpanded,
	} = useDeliveryManagement({ portfolio: project });

	return (
		<Box>
			<DeliveryHeader onAddDelivery={handleAddDelivery} />

			{/* Render delivery sections instead of a grid */}
			<Box sx={{ mt: 2 }}>
				{deliveries.map((delivery) => {
					const isExpanded = expandedDeliveries.has(delivery.id);
					const features = loadedFeatures.get(delivery.id) || [];
					const isLoadingFeatures = loadingFeaturesByDelivery.has(delivery.id);

					return (
						<DeliverySection
							key={delivery.id}
							delivery={delivery}
							features={features}
							isExpanded={isExpanded}
							isLoadingFeatures={isLoadingFeatures}
							onToggleExpanded={handleToggleExpanded}
							onDelete={handleDeleteDelivery}
						/>
					);
				})}
			</Box>

			<DeliveryModals
				portfolio={project}
				showCreateModal={showCreateModal}
				selectedDelivery={selectedDelivery}
				deliveryToDelete={deliveryToDelete}
				deleteDialogOpen={deleteDialogOpen}
				onCloseCreateModal={handleCloseCreateModal}
				onCloseEditModal={handleCloseEditModal}
				onDeliveryUpdate={onDeliveryUpdate}
				onDeleteConfirmation={handleDeleteConfirmation}
			/>
		</Box>
	);
};

export default PortfolioDeliveryView;
