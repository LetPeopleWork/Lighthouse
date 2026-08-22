import { Box } from "@mui/material";
import type React from "react";
import { useLicenseRestrictions } from "../../../hooks/useLicenseRestrictions";
import type { Portfolio } from "../../../models/Portfolio/Portfolio";
import {
	ArchivedDeliveriesSection,
	DeliveryHeader,
	DeliveryModals,
	useDeliveryManagement,
} from "./Components/DeliveryGrid";
import DeliverySection from "./Components/DeliveryGrid/DeliverySection";

interface PortfolioDeliveryViewProps {
	portfolio: Portfolio;
	canEdit?: boolean;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	portfolio,
	canEdit = true,
}) => {
	const {
		deliveries,
		archivedDeliveries,
		showCreateModal,
		selectedDelivery,
		deleteDialogOpen,
		deliveryToDelete,
		archiveDialogOpen,
		deliveryToArchive,
		handleAddDelivery,
		handleDeleteDelivery,
		handleEditDelivery,
		handleArchiveDelivery,
		handleArchiveConfirmation,
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

	// Asked once for the whole list rather than per row: the answer is the same for every Delivery
	// on the page, and asking per row means a licence lookup for each one.
	const { licenseStatus } = useLicenseRestrictions();
	const canArchive = licenseStatus?.canUsePremiumFeatures ?? false;

	return (
		<Box>
			{canEdit && <DeliveryHeader onAddDelivery={handleAddDelivery} />}

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
							onArchive={handleArchiveDelivery}
							teams={portfolio.involvedTeams}
							canEdit={canEdit}
							canArchive={canArchive}
						/>
					);
				})}

				<ArchivedDeliveriesSection
					archivedDeliveries={archivedDeliveries}
					canEdit={canEdit}
					onDelete={handleDeleteDelivery}
				/>
			</Box>

			{canEdit && (
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
					deliveryToArchive={deliveryToArchive}
					archiveDialogOpen={archiveDialogOpen}
					onDeleteConfirmation={handleDeleteConfirmation}
					onArchiveConfirmation={handleArchiveConfirmation}
				/>
			)}
		</Box>
	);
};

export default PortfolioDeliveryView;
