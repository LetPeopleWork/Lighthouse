import type React from "react";
import DeleteConfirmationDialog from "../../../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { Portfolio } from "../../../../../models/Portfolio/Portfolio";
import ArchiveConfirmationDialog from "./ArchiveConfirmationDialog";
import { DeliveryCreateModal } from "./DeliveryCreateModal";
import type { DeletableDelivery } from "./useDeliveryManagement";

interface DeliveryModalsProps {
	portfolio: Portfolio;
	showCreateModal: boolean;
	selectedDelivery: Delivery | null;
	deliveryToDelete: DeletableDelivery | null;
	deleteDialogOpen: boolean;
	deliveryToArchive: Delivery | null;
	archiveDialogOpen: boolean;
	onCloseCreateModal: () => void;
	onCloseEditModal: () => void;
	onCreateDelivery: (deliveryData: {
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: number;
		rules?: { fieldKey: string; operator: string; value: string }[];
		mode?: "and" | "or";
		sourceKey?: string;
		sourceReference?: string;
	}) => Promise<void>;
	onUpdateDelivery: (deliveryData: {
		id: number;
		name: string;
		date: string;
		featureIds: number[];
		selectionMode?: number;
		rules?: { fieldKey: string; operator: string; value: string }[];
		mode?: "and" | "or";
		sourceKey?: string;
		sourceReference?: string;
	}) => Promise<void>;
	onDeleteConfirmation: (confirmed: boolean) => void;
	onArchiveConfirmation: (confirmed: boolean, stopAsking?: boolean) => void;
}

export const DeliveryModals: React.FC<DeliveryModalsProps> = ({
	portfolio,
	showCreateModal,
	selectedDelivery,
	deliveryToDelete,
	deleteDialogOpen,
	deliveryToArchive,
	archiveDialogOpen,
	onCloseCreateModal,
	onCloseEditModal,
	onCreateDelivery,
	onUpdateDelivery,
	onDeleteConfirmation,
	onArchiveConfirmation,
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
					onCancel={() => onDeleteConfirmation(false)}
					onConfirm={() => onDeleteConfirmation(true)}
				/>
			)}

			{deliveryToArchive && (
				<ArchiveConfirmationDialog
					open={archiveDialogOpen}
					itemName={deliveryToArchive.name}
					onCancel={() => onArchiveConfirmation(false)}
					onConfirm={(stopAsking) => onArchiveConfirmation(true, stopAsking)}
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
