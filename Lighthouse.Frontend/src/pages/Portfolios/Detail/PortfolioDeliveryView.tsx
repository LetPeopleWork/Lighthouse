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
	portfolio: Portfolio;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	portfolio,
}) => {
	const {
		deliveries,
		showCreateModal,
		selectedDelivery,
		deleteDialogOpen,
		deliveryToDelete,
		handleAddDelivery,
		handleDeleteDelivery,
		handleEditDelivery,
		handleDeleteConfirmation,
		handleCloseCreateModal,
		handleCloseEditModal,
		handleCreateDelivery,
		handleUpdateDelivery,
		expandedDeliveries,
		loadedFeatures,
		loadingFeaturesByDelivery,
		handleToggleExpanded,
	} = useDeliveryManagement({ portfolio });

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
							onEdit={handleEditDelivery}
							teams={portfolio.involvedTeams}
						/>
					);
				})}
			</Box>

			<DeliveryModals
				portfolio={portfolio}
				showCreateModal={showCreateModal}
				selectedDelivery={selectedDelivery}
				deliveryToDelete={deliveryToDelete}
				deleteDialogOpen={deleteDialogOpen}
				onCloseCreateModal={handleCloseCreateModal}
				onCloseEditModal={handleCloseEditModal}
				onCreateDelivery={handleCreateDelivery}
				onUpdateDelivery={handleUpdateDelivery}
				onDeleteConfirmation={handleDeleteConfirmation}
			/>
		</Box>
	);
};

export default PortfolioDeliveryView;
