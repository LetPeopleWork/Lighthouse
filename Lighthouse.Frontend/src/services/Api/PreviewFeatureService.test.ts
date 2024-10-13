import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { PreviewFeatureService } from './PreviewFeatureService';
import { PreviewFeature } from '../../models/Preview/PreviewFeature';

vi.mock('axios');
const mockedAxios = vi.mocked(axios, true);

describe('PreviewFeatureService', () => {
    let previewFeatureService: PreviewFeatureService;

    beforeEach(() => {
        mockedAxios.create.mockReturnThis();
        previewFeatureService = new PreviewFeatureService();
    });

    afterEach(() => {
        vi.resetAllMocks();
    });

    it('should get all preview features', async () => {
        const mockResponse = [
            new PreviewFeature(1, 'feature-001', 'Feature 1', 'Description 1', true),
            new PreviewFeature(2, 'feature-002', 'Feature 2', 'Description 2', false),
        ];

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const result = await previewFeatureService.getAllFeatures();

        expect(result).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith('/previewfeatures');
    });

    it('should get a feature by key', async () => {
        const key = 'feature-001';
        const mockResponse = new PreviewFeature(1, key, 'Feature 1', 'Description 1', true);

        mockedAxios.get.mockResolvedValueOnce({ data: mockResponse });

        const result = await previewFeatureService.getFeatureByKey(key);

        expect(result).toEqual(mockResponse);
        expect(mockedAxios.get).toHaveBeenCalledWith(`/previewfeatures/${key}`);
    });

    it('should return null if feature by key does not exist', async () => {
        const key = 'feature-003';

        mockedAxios.get.mockResolvedValueOnce({ data: null });

        const result = await previewFeatureService.getFeatureByKey(key);

        expect(result).toBeNull();
        expect(mockedAxios.get).toHaveBeenCalledWith(`/previewfeatures/${key}`);
    });

    it('should update a preview feature', async () => {
        const feature = new PreviewFeature(1, 'feature-001', 'Feature 1', 'Updated Description', true);

        mockedAxios.post.mockResolvedValueOnce({});

        await previewFeatureService.updateFeature(feature);

        expect(mockedAxios.post).toHaveBeenCalledWith(`/previewfeatures/${feature.id}`, feature);
    });

    it('should throw an error if API call fails', async () => {
        const feature = new PreviewFeature(1, 'feature-001', 'Feature 1', 'Description', true);

        mockedAxios.post.mockRejectedValueOnce(new Error('API error'));

        await expect(previewFeatureService.updateFeature(feature))
            .rejects
            .toThrow('API error');

        expect(mockedAxios.post).toHaveBeenCalledWith(`/previewfeatures/${feature.id}`, feature);
    });
});
