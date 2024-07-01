import React, { useEffect, useState } from 'react';

interface LoadingAnimationProps {
    asyncFunction: () => Promise<void>;
    children: React.ReactNode;
}

const LoadingAnimation: React.FC<LoadingAnimationProps> = ({ asyncFunction, children }) => {
    const [isLoading, setIsLoading] = useState(true);
    const [hasError, setHasError] = useState(false);

    useEffect(() => {
        const loadData = async () => {
            try {
                await asyncFunction();
                setIsLoading(false);
            } catch (error) {
                console.error('Error during async function execution:', error);
                setHasError(true);
            }
        };

        loadData();
    }, [asyncFunction]);

    if (isLoading) {
        return <div>Loading...</div>;
    }

    if (hasError) {
        return <div>Error loading data. Please try again later.</div>;
    }

    return <>{children}</>;
};

export default LoadingAnimation;
