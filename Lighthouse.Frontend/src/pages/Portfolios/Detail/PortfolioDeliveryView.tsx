import type React from "react";
import type { Portfolio } from "../../../models/Portfolio/Portfolio";
import {
	DeliveryGrid,
	DeliveryHeader,
	DeliveryModals,
	useDeliveryManagement,
} from "./Components/DeliveryGrid";

interface PortfolioDeliveryViewProps {
	project: Portfolio;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	project,
}) => {
	const {
		deliveries,
		isLoading,
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
	} = useDeliveryManagement({ portfolio: project });

	return (
		<>
			<DeliveryHeader onAddDelivery={handleAddDelivery} />
			<DeliveryGrid
				deliveries={deliveries}
				isLoading={isLoading}
				onDelete={handleDeleteDelivery}
			/>
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
		</>
	);
};

export default PortfolioDeliveryView;
