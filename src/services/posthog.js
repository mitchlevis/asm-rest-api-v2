import { PostHog } from 'posthog-node'

let initializationPromise = null;

let env = null;
let ctx = null;
let posthog = null;

let distinctId = null;

export function usePosthog() {

	const initialize = async (request, workerEnv, workerCtx) => {
		console.log('Initializing Posthog');
		if(initializationPromise) {
			console.log('Posthog already initializing');
			return initializationPromise;
		}

		env = workerEnv;
		ctx = workerCtx;

		if(!posthog) {
			posthog = new PostHog(env.POSTHOG_API_KEY, {
				host: env.POSTHOG_HOST,
				flushAt: 1, // Send events immediately in edge environment
				flushInterval: 0, // Don't wait for interval
			})
		}

		// Attempt to get the distinct id from the request
		distinctId = request.headers.get('x-username') || request.headers.get('X-Username');
		console.log('Identifying User with distinctId:', distinctId);
		console.log('Capturing API Event');

		// Create a promise that resolves when initialization is complete
		initializationPromise = new Promise((resolve) => {
			const capturePromise = posthog.captureImmediate({
				distinctId: distinctId,
				event: 'api_event',
				properties: {
					$current_url: request.url,
					$ip: request.headers.get('x-forwarded-for') || request.headers.get('X-Forwarded-For'),
					$user_agent: request.headers.get('user-agent') || request.headers.get('User-Agent'),
					$referer: request.headers.get('referer') || request.headers.get('Referer'),
					$session_id: request.headers.get('x-session-token') || request.headers.get('X-Session-Token'),
					$username: distinctId,
					$environment: env.ENVIRONMENT,
					$version: env.VERSION,
				}
			});

			ctx.waitUntil(capturePromise);

			// Resolve immediately after scheduling, since PostHog is ready to use
			resolve();
		});

		return initializationPromise;
	}

	const capture = async (request, event, customProperties) => {
		console.log('Capturing Event:', event, 'with Properties:', customProperties);

		if(!posthog && !initializationPromise) {
			console.log('Posthog not initialized');
			return;
		}

		if(initializationPromise) {
			console.log('Posthog still initializing, waiting for completion');
			try {
				// Wait for initialization with a 5 second timeout
				await Promise.race([
					initializationPromise,
					new Promise((_, reject) => setTimeout(() => reject(new Error('PostHog initialization timeout')), 5000))
				]);
				console.log('Posthog initialized, capturing event');
			} catch (initError) {
				console.error('Error waiting for PostHog initialization:', initError);
				// Continue anyway - PostHog should be ready to use
			}
		}

		if(!posthog) {
			console.log('Posthog not available, skipping capture');
			return;
		}

		ctx.waitUntil(
			posthog.captureImmediate({
				distinctId: distinctId,
				event: event,
				properties: {
					// PH Properties
					$current_url: request.url,
					$ip: request.headers.get('x-forwarded-for') || request.headers.get('X-Forwarded-For'),
					$user_agent: request.headers.get('user-agent') || request.headers.get('User-Agent'),
					$referer: request.headers.get('referer') || request.headers.get('Referer'),
					$session_id: request.headers.get('x-session-token') || request.headers.get('X-Session-Token'),
					$username: distinctId,
					$environment: env.ENVIRONMENT,
					$version: env.VERSION,

					// Custom Properties
					...customProperties,
				}
			})
		);
	}

	const captureError = async (request, error) => {
		console.log('Capturing Error for:', distinctId);

		if(!posthog && !initializationPromise) {
			console.log('Posthog not initialized');
			return;
		}

		if(initializationPromise) {
			console.log('Posthog still initializing, waiting for completion');
			try {
				// Wait for initialization with a 5 second timeout
				await Promise.race([
					initializationPromise,
					new Promise((_, reject) => setTimeout(() => reject(new Error('PostHog initialization timeout')), 5000))
				]);
				console.log('Posthog initialized, capturing error');
			} catch (initError) {
				console.error('Error waiting for PostHog initialization:', initError);
				// Continue anyway - PostHog should be ready to use
			}
		}

		if(!posthog) {
			console.log('Posthog not available, skipping error capture');
			return;
		}

		ctx.waitUntil(
			posthog.captureException(error, distinctId, {
				$current_url: request.url,
				$ip: request.headers.get('x-forwarded-for') || request.headers.get('X-Forwarded-For'),
				$user_agent: request.headers.get('user-agent') || request.headers.get('User-Agent'),
				$referer: request.headers.get('referer') || request.headers.get('Referer'),
				$session_id: request.headers.get('x-session-token') || request.headers.get('X-Session-Token'),
				$username: distinctId,
				$environment: env.ENVIRONMENT,
				$version: env.VERSION,
			})
		);
	}

	return {
		initialize,
		capture,
		captureError,
	}

}
