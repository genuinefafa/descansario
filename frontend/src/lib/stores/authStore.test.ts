import { describe, it, expect, vi, beforeEach } from 'vitest';
import { get } from 'svelte/store';
import { authStore } from './authStore';
import { authService } from '$lib/services/auth';

// Mock the auth service
vi.mock('$lib/services/auth', () => ({
  authService: {
    login: vi.fn(),
    logout: vi.fn(),
    register: vi.fn(),
    getUser: vi.fn(),
    isAuthenticated: vi.fn(),
    getCurrentUser: vi.fn(),
  },
}));

describe('authStore', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset store to initial state by logging out
    authStore.logout();
  });

  describe('initial state', () => {
    it('should have correct initial state after logout', () => {
      const state = get(authStore);
      expect(state.user).toBeNull();
      expect(state.loading).toBe(false);
      expect(state.error).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });

  describe('login', () => {
    it('should set user and isAuthenticated on successful login', async () => {
      const mockUser = {
        id: 1,
        email: 'test@test.com',
        name: 'Test User',
        role: 'User' as const,
        personId: 1,
        personName: 'Test Person',
      };

      vi.mocked(authService.login).mockResolvedValue({
        token: 'test-token',
        user: mockUser,
      });

      const result = await authStore.login('test@test.com', 'password123');

      expect(result).toBe(true);
      const state = get(authStore);
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
      expect(state.loading).toBe(false);
      expect(state.error).toBeNull();
    });

    it('should set error on failed login', async () => {
      vi.mocked(authService.login).mockRejectedValue(new Error('Invalid credentials'));

      const result = await authStore.login('test@test.com', 'wrong-password');

      expect(result).toBe(false);
      const state = get(authStore);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(state.loading).toBe(false);
      expect(state.error).toBe('Invalid credentials');
    });

    it('should set loading to true during login', async () => {
      // Create a promise we can control
      let resolveLogin: (value: unknown) => void;
      const loginPromise = new Promise((resolve) => {
        resolveLogin = resolve;
      });
      vi.mocked(authService.login).mockReturnValue(loginPromise as never);

      // Start login (don't await)
      const loginResultPromise = authStore.login('test@test.com', 'password');

      // Check loading state
      const loadingState = get(authStore);
      expect(loadingState.loading).toBe(true);

      // Resolve the login
      resolveLogin!({ token: 'test', user: { id: 1, email: 'test@test.com', name: 'Test', role: 'User' } });
      await loginResultPromise;

      // Check final state
      const finalState = get(authStore);
      expect(finalState.loading).toBe(false);
    });
  });

  describe('logout', () => {
    it('should clear user and set isAuthenticated to false', async () => {
      // First login
      const mockUser = {
        id: 1,
        email: 'test@test.com',
        name: 'Test User',
        role: 'User' as const,
        personId: null,
        personName: null,
      };
      vi.mocked(authService.login).mockResolvedValue({
        token: 'test-token',
        user: mockUser,
      });
      await authStore.login('test@test.com', 'password');

      // Then logout
      authStore.logout();

      const state = get(authStore);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(authService.logout).toHaveBeenCalled();
    });
  });

  describe('clearError', () => {
    it('should clear error state', async () => {
      // Cause an error
      vi.mocked(authService.login).mockRejectedValue(new Error('Test error'));
      await authStore.login('test@test.com', 'password');

      // Verify error exists
      let state = get(authStore);
      expect(state.error).toBe('Test error');

      // Clear error
      authStore.clearError();

      state = get(authStore);
      expect(state.error).toBeNull();
    });
  });

  describe('init', () => {
    it('should set user when already authenticated', async () => {
      const mockUser = {
        id: 1,
        email: 'test@test.com',
        name: 'Test User',
        role: 'User' as const,
        personId: 1,
        personName: 'Test Person',
      };

      vi.mocked(authService.getUser).mockReturnValue(mockUser);
      vi.mocked(authService.isAuthenticated).mockReturnValue(true);
      vi.mocked(authService.getCurrentUser).mockResolvedValue(mockUser);

      await authStore.init();

      const state = get(authStore);
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
      expect(state.loading).toBe(false);
    });

    it('should clear user when not authenticated', async () => {
      vi.mocked(authService.getUser).mockReturnValue(null);
      vi.mocked(authService.isAuthenticated).mockReturnValue(false);

      await authStore.init();

      const state = get(authStore);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });

    it('should handle init errors gracefully', async () => {
      vi.mocked(authService.getUser).mockReturnValue({ id: 1 } as never);
      vi.mocked(authService.isAuthenticated).mockReturnValue(true);
      vi.mocked(authService.getCurrentUser).mockRejectedValue(new Error('Token expired'));

      await authStore.init();

      const state = get(authStore);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(state.loading).toBe(false);
    });
  });
});
