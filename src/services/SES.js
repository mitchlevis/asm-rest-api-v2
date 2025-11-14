import { AwsClient } from 'aws4fetch'

export function useSESService() {

	const sendEmail = async ({ fromEmailAddress, toEmailList = [], bccEmailList = [], subject, textBody, htmlBody }) => {
		try{
			console.log('Creating AWS Client');
			const aws = new AwsClient({ accessKeyId: process.env.AWS_ACCESS_KEY_ID, secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY});

			console.log(`Sending email to ${toEmailList.join(', ')} with subject ${subject}`);
			let resp = await aws.fetch(`https://email.${process.env.AWS_SES_REGION}.amazonaws.com/v2/email/outbound-emails`, {
				method: 'POST',
				headers: {
					'content-type': 'application/json',
				},
				body: JSON.stringify({
					Destination:
					{
						ToAddresses: toEmailList,
						BccAddresses: bccEmailList,
					},
					FromEmailAddress: fromEmailAddress,
					Content: {
						Simple: {
							Subject: {
								Data: subject
							},
							Body: {
								Text: textBody && textBody !== '' ? {
									Data: textBody.replace(/<br\s*[\/]?>/gi, "\n"),
								} : undefined,
								Html: htmlBody && htmlBody !== '' ? {
									Data: htmlBody,
								} : undefined,
							}
						},
					},
				}),
			});

			const respText = await resp.json();
			console.log(resp.status + " " + resp.statusText);
			console.log(respText);

			if (resp.status != 200 && resp.status != 201) {
				throw new Error('Error sending email: ' + resp.status + " " + resp.statusText + " " + respText);
			}

			return { status: resp.status, message: resp.statusText };
		}
		catch(err){
			console.error(`Error sending email: ${err}`);
			throw err;
		}
	}

	return { sendEmail };
}
