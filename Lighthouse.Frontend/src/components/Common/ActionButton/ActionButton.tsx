import { Button, CircularProgress } from "@mui/material";
import React from "react";

type ButtonVariant = "text" | "outlined" | "contained";

interface ActionButtonProps {
    buttonText: string;
    isWaiting: boolean;
    onClickHandler: () => void;
    buttonVariant?: ButtonVariant;
}

const ActionButton: React.FC<ActionButtonProps> = ({ buttonText, isWaiting, onClickHandler, buttonVariant = "contained" }) => {
    return (
        <Button 
            variant={buttonVariant} 
            onClick={onClickHandler} 
            disabled={isWaiting}
            sx={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
        >
            {isWaiting && (
                <CircularProgress 
                    size={24} 
                    sx={{ position: 'absolute' }} 
                />
            )}
            {buttonText}
        </Button>
    );
};

export default ActionButton;
