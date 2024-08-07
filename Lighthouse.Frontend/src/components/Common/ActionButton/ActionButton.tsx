import { Button, CircularProgress } from "@mui/material";
import React, { useState } from "react";

type ButtonVariant = "text" | "outlined" | "contained";

interface ActionButtonProps {
    buttonText: string;
    onClickHandler: () => Promise<void>;
    buttonVariant?: ButtonVariant;
    disabled? : boolean;
    maxHeight? : string;
}

const ActionButton: React.FC<ActionButtonProps> = ({ buttonText, onClickHandler, buttonVariant = "contained", disabled = false, maxHeight }) => {
    const [isWaiting, setIsWaiting] = useState<boolean>(false);

    const handleClick = async () => {
        setIsWaiting(true);
        await onClickHandler();
        setIsWaiting(false);
    }

    return (
        <Button 
            variant={buttonVariant} 
            onClick={handleClick} 
            disabled={disabled || isWaiting}
            sx={{ position: 'relative', display: 'flex', alignItems: 'center', justifyContent: 'center', maxHeight: {maxHeight} }}
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
