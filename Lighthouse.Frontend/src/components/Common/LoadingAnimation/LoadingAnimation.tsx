import { CircularProgress } from '@mui/material';
import React from 'react';

interface LoadingAnimationProps {
    isLoading: boolean;
    hasError: boolean;
    children: React.ReactNode;
}

const LoadingAnimation: React.FC<LoadingAnimationProps> = ({ isLoading, hasError, children }) => {

    if (isLoading) {
        return <CircularProgress  />
    }

    if (hasError) {
        return <div>Error loading data. Please try again later.</div>;
    }

    return <>{children}</>;
};

export default LoadingAnimation;
