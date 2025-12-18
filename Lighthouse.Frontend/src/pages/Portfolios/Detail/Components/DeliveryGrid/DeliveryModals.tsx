import type React from "react";
import DeleteConfirmationDialog from "../../../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { DeliveryCreateModal } from "./DeliveryCreateModal";

interface DeliveryModalsProps {
	portfolio: Portfolio;
	showCreateModal: boolean;
	selectedDelivery: Delivery | null;
	deliveryToDelete: Delivery | null;
	deleteDialogOpen: boolean;
	onCloseCreateModal: () => void;
	onCloseEditModal: () => void;
	onCreateDelivery: (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
	}) => Promise<void>;
	onUpdateDelivery: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
	}) => Promise<void>;
	onDeleteConfirmation: (confirmed: boolean) => void;
}

export const DeliveryModals: React.FC<DeliveryModalsProps> = ({
	portfolio,
	showCreateModal,
	selectedDelivery,
	deliveryToDelete,
	deleteDialogOpen,
	onCloseCreateModal,
	onCloseEditModal,
	onCreateDelivery,
	onUpdateDelivery,
	onDeleteConfirmation,
}) => {
	const isModalOpen = showCreateModal || !!selectedDelivery;
	const editingDelivery = selectedDelivery;

	const handleClose = () => {
		if (selectedDelivery) {
			onCloseEditModal();
		} else {
			onCloseCreateModal();
		}
	};

	return (
		<>
			{deliveryToDelete && (
				<DeleteConfirmationDialog
					open={deleteDialogOpen}
					itemName={deliveryToDelete.name}
					onClose={onDeleteConfirmation}
				/>
			)}

			<DeliveryCreateModal
				open={isModalOpen}
				portfolio={portfolio}
				editingDelivery={editingDelivery}
				onClose={handleClose}
				onSave={onCreateDelivery}
				onUpdate={onUpdateDelivery}
			/>
		</>
	);
};

export default DeliveryModals;
