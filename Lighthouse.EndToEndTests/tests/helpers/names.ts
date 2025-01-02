export function generateRandomName(length = 10): string {
	const characters = "abcdefghijklmnopqrstuvwxyz";
	let result = "";
	for (let i = 0; i < length; i++) {
		result += characters.charAt(Math.floor(Math.random() * characters.length));
	}
	return `E2ETests_${result}`;
}
