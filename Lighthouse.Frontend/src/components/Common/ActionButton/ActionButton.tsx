import { Button, CircularProgress } from "@mui/material";
import React from "react";

interface ActionButtonProps {
    buttonText: string;
    isWaiting: boolean;
    onClickHandler: () => void;
}

const ActionButton: React.FC<ActionButtonProps> = ({ buttonText, isWaiting, onClickHandler }) => {
    return (
        <Button variant="contained" onClick={onClickHandler} disabled={isWaiting}>
            {isWaiting ? <CircularProgress /> : buttonText}
        </Button>
    )
}

export default ActionButton