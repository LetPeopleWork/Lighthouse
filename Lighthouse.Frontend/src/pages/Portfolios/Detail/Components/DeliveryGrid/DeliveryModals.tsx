import type React from "react";
import DeleteConfirmationDialog from "../../../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";

interface DeliveryModalsProps {
	portfolio: Portfolio;
	showCreateModal: boolean;
	selectedDelivery: Delivery | null;
	deliveryToDelete: Delivery | null;
	deleteDialogOpen: boolean;
	onCloseCreateModal: () => void;
	onCloseEditModal: () => void;
	onDeliveryUpdate: () => void;
	onDeleteConfirmation: (confirmed: boolean) => void;
}

export const DeliveryModals: React.FC<DeliveryModalsProps> = ({
	deliveryToDelete,
	deleteDialogOpen,
	onDeleteConfirmation,
}) => {
	return (
		<>
			{deliveryToDelete && (
				<DeleteConfirmationDialog
					open={deleteDialogOpen}
					itemName={deliveryToDelete.name}
					onClose={onDeleteConfirmation}
				/>
			)}
			{/* TODO: Add DeliveryEditModal when it exists */}
			{/* TODO: Add DeliveryCreateModal when it exists */}
		</>
	);
};

export default DeliveryModals;
