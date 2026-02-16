import { Box } from "@mui/material";
import type React from "react";
import { useEffect, useState } from "react";

interface ImageComponentProps {
	src: string;
	alt?: string;
}

const ImageComponent: React.FC<ImageComponentProps> = ({ src, alt }) => {
	const [imageDimensions, setImageDimensions] = useState<{
		width: number;
		height: number;
	}>({ width: 0, height: 0 });

	useEffect(() => {
		const img = new Image();
		img.src = src;
		img.onload = () => {
			setImageDimensions({ width: img.width, height: img.height });
		};
	}, [src]);

	return (
		<Box
			component="img"
			sx={{
				width: "100%",
				height: "auto",
				maxWidth: "100%",
				objectFit: "contain",
				aspectRatio:
					imageDimensions.width && imageDimensions.height
						? imageDimensions.width / imageDimensions.height
						: undefined,
				maxHeight:
					imageDimensions.height > 0
						? `${imageDimensions.height}px`
						: undefined,
				borderRadius: 1,
				boxShadow: 1,
				marginTop: 2,
			}}
			src={src}
			alt={alt ?? "Image"}
		/>
	);
};

export default ImageComponent;
