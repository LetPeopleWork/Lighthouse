import { PreviewFeature } from './PreviewFeature';

describe('PreviewFeature Class', () => {
    let previewFeature: PreviewFeature;
    let id: number;
    let key: string;
    let name: string;
    let description: string;
    let enabled: boolean;

    beforeEach(() => {
        id = 1;
        key = 'feature-001';
        name = 'Feature 1';
        description = 'This is the description for Feature 1.';
        enabled = true;

        previewFeature = new PreviewFeature(id, key, name, description, enabled);
    });

    it('should create an instance of PreviewFeature correctly', () => {
        expect(previewFeature).toBeInstanceOf(PreviewFeature);
        expect(previewFeature.id).toBe(id);
        expect(previewFeature.key).toBe(key);
        expect(previewFeature.name).toBe(name);
        expect(previewFeature.description).toBe(description);
        expect(previewFeature.enabled).toBe(enabled);
    });

    it('should set the enabled property correctly', () => {
        expect(previewFeature.enabled).toBe(true);
        previewFeature.enabled = false;
        expect(previewFeature.enabled).toBe(false);
    });

    it('should handle description updates', () => {
        expect(previewFeature.description).toBe('This is the description for Feature 1.');
        previewFeature.description = 'Updated description for Feature 1.';
        expect(previewFeature.description).toBe('Updated description for Feature 1.');
    });
});
