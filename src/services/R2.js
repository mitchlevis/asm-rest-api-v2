let env = null;

export function useR2Service(){

	const initialize = (workerEnv) => {
		console.log('Initializing R2 Service');
		env = workerEnv;
		console.log('R2 Service initialized');
	}

	const getObject = async ({key, bucketBindingName, json = true}) => {
		try{
			if(!env?.API_BUCKET) {
				throw new Error(`R2 Binding [${bucketBindingName}] not found in env`);
			}
			console.log(`Getting object from bucket ${bucketBindingName} with key ${key}`);

			const object = await env[bucketBindingName].get(key);
			if(!object) {
				return null;
			}

			if(json) {
				// R2 get() returns a GetResult object with a body stream
				// We need to call .json() on the body to parse it
				return await object.json();
			}
			return object;
		}
		catch(err){
			console.error(`Error getting object from ${bucketBindingName} with key ${key}: ${err}`);
			throw err;
		}
	}

	const putObject = async ({key, data, bucketBindingName, json = false}) => {
		try{
			if(!env?.API_BUCKET) {
				throw new Error(`R2 Binding [${bucketBindingName}] not found in env`);
			}
			console.log(`Putting object into ${bucketBindingName} with key ${key}`);
			console.log(`Data to store:`, data);
			console.log(`JSON mode: ${json}`);

			const options = {};
			if(json) {
				options.httpMetadata = {
					contentType: 'application/json'
				};
			}

			const dataToStore = json ? JSON.stringify(data) : data;
			console.log(`Data after stringify:`, dataToStore);
			console.log(`Bucket binding available:`, !!env[bucketBindingName]);

			const result = await env[bucketBindingName].put(key, dataToStore, options);
			console.log(`R2 put result:`, result);
			return result;
		}
		catch(err){
			console.error(`Error putting object into ${bucketBindingName} with key ${key}: ${err}`);
			throw err;
		}
	}

	const deleteObject = async ({key, bucketBindingName}) => {
		try{
			if(!env?.API_BUCKET) {
				throw new Error(`R2 Binding [${bucketBindingName}] not found in env`);
			}
			console.log(`Deleting object from ${bucketBindingName} with key ${key}`);

			return env[bucketBindingName].delete(key);
		}
		catch(err){
			console.error(`Error deleting object from ${bucketBindingName} with key ${key}: ${err}`);
			throw err;
		}
	}

	return {
		initialize,
		getObject,
		putObject,
		deleteObject,
	}
}
