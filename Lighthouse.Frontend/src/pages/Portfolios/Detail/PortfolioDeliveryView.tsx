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
		deleteDialogOpen,
		deliveryToDelete,
		handleAddDelivery,
		handleDeleteDelivery,
		handleDeleteConfirmation,
		handleCloseCreateModal,
		handleCreateDelivery,
		expandedDeliveries,
		loadedFeatures,
		loadingFeaturesByDelivery,
		handleToggleExpanded,
	} = useDeliveryManagement({ portfolio });

	return (
		<Box>
			<DeliveryHeader
				onAddDelivery={handleAddDelivery}
				deliveries={deliveries}
			/>

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
							teams={portfolio.involvedTeams}
						/>
					);
				})}
			</Box>

			<DeliveryModals
				portfolio={portfolio}
				showCreateModal={showCreateModal}
				deliveryToDelete={deliveryToDelete}
				deleteDialogOpen={deleteDialogOpen}
				onCloseCreateModal={handleCloseCreateModal}
				onCreateDelivery={handleCreateDelivery}
				onDeleteConfirmation={handleDeleteConfirmation}
			/>
		</Box>
	);
};

export default PortfolioDeliveryView;
