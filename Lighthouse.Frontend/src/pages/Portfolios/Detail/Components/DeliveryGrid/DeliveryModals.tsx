import type React from "react";
import DeleteConfirmationDialog from "../../../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import { DeliveryCreateModal } from "./DeliveryCreateModal";

interface DeliveryModalsProps {
	portfolio: Portfolio;
	showCreateModal: boolean;
	deliveryToDelete: Delivery | null;
	deleteDialogOpen: boolean;
	onCloseCreateModal: () => void;
	onCreateDelivery: (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
	}) => Promise<void>;
	onDeleteConfirmation: (confirmed: boolean) => void;
}

export const DeliveryModals: React.FC<DeliveryModalsProps> = ({
	portfolio,
	showCreateModal,
	deliveryToDelete,
	deleteDialogOpen,
	onCloseCreateModal,
	onCreateDelivery,
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

			<DeliveryCreateModal
				open={showCreateModal}
				portfolio={portfolio}
				onClose={onCloseCreateModal}
				onSave={onCreateDelivery}
			/>

			{/* TODO: Add DeliveryEditModal when it exists */}
		</>
	);
};

export default DeliveryModals;
