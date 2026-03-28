import java.util.Scanner;
import java.util.StringJoiner;

public class timeConverter {

    public static void main(String[] args) {
        Scanner scanner = new Scanner(System.in);

        System.out.print("Enter frames per second (fps): ");
        int fps = scanner.nextInt();
        scanner.nextLine(); // consume newline
        // Your input as an array of strings    
        String[] timecodes = {
            "01:00:35:08","01:00:36:10","01:00:40:16","01:00:41:12","01:00:41:16","01:00:42:23","01:00:48:12","01:00:50:00","01:00:50:02","01:00:51:00","01:00:55:17","01:00:57:09","01:00:57:12","01:00:58:13","01:01:10:09","01:01:12:04","01:01:12:06","01:01:14:03","01:01:38:14","01:01:39:12","01:01:47:24","01:01:51:00","01:01:54:23","01:01:56:13","01:01:56:15","01:01:57:12","01:02:02:03","01:02:03:20","01:02:04:00","01:02:04:24","01:02:16:22","01:02:18:12","01:02:18:19","01:02:20:15","01:02:20:17","01:02:22:06","01:02:22:07","01:02:23:12","01:02:24:06","01:02:25:22","01:02:26:02","01:02:27:05","01:02:27:24","01:02:29:17","01:02:29:18","01:02:31:00","01:02:31:16","01:02:33:10","01:02:33:12","01:02:34:15","01:02:40:21","01:02:42:03","01:02:47:00","01:02:50:01","01:02:54:00","01:02:55:15","01:02:55:17","01:02:56:14","01:03:01:06","01:03:02:21","01:03:02:23","01:03:04:03","01:03:11:07","01:03:12:10","01:03:15:24","01:03:17:17","01:03:17:20","01:03:19:11","01:03:19:20","01:03:21:08","01:03:21:12","01:03:22:17","01:03:23:10","01:03:25:03","01:03:25:06","01:03:26:07","01:03:27:03","01:03:28:19","01:03:28:21","01:03:29:24","01:03:30:17","01:03:32:11","01:03:32:13","01:03:33:17","01:03:47:20","01:03:49:14",
        };

        String result = buildExpressions(timecodes, fps);
        System.out.println(result);

        scanner.close();
    }

    // Input: array of "HH:MM:SS:FF"
    // Output: "MM * 60 + SS + (float)FF / fps,MM * 60 + SS + (float)FF / fps,..."
    public static String buildExpressions(String[] timecodes, int fps) {
        StringJoiner joiner = new StringJoiner(",");

        for (String raw : timecodes) {
            if (raw == null) continue;

            String tc = raw.trim();

            if (tc.isEmpty()) continue;

            String[] parts = tc.split(":");
            if (parts.length != 4) continue;  // skip invalid

            String mm = parts[1];
            String ss = parts[2];
            String ff = parts[3];

            String expr = String.format(
                "%s * 60 + %s + (float)%s / %d",
                mm, ss, ff, fps
            );
            joiner.add(expr);
        }

        return joiner.toString();
    }
}