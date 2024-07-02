import React from 'react';
import { SyncLoader } from 'react-spinners';

interface LoadingAnimationProps {
    isLoading: boolean;
    hasError: boolean;
    children: React.ReactNode;
}

const LoadingAnimation: React.FC<LoadingAnimationProps> = ({ isLoading, hasError, children }) => {

    if (isLoading) {
        return <SyncLoader />
    }

    if (hasError) {
        return <div>Error loading data. Please try again later.</div>;
    }

    return <>{children}</>;
};

export default LoadingAnimation;
