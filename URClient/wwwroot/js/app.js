var tempChart = null;
var jointChart = null;

window.refreshCharts = (temps, joints) => {
    var ctx_temp = document.getElementById('ur_temps');
    var ctx_joint = document.getElementById('ur_joints');

    if (tempChart == null) {
        tempChart = new Chart(ctx_temp, {
            type: 'bar',
            data: {
                labels: ['base', 'shoulder', 'elbow', 'wrist1', 'wrist2', 'wrist3',],
                datasets: [{
                    label: 'Temperature of Joints',
                    data: [temps[0], temps[1], temps[2], temps[3], temps[4], temps[5]],
                }]
            },
            options: {
                indexAxis: 'y',
                scales: {
                    x: {
                        suggestedMin: 0,
                        suggestedMax: 70
                    },
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }
    else {

        tempChart.data.datasets[0].data = [temps[0], temps[1], temps[2], temps[3], temps[4], temps[5]];

        tempChart.update();
    }

    if (jointChart == null) {
        jointChart = new Chart(ctx_joint, {
            type: 'bar',
            data: {
                labels: ['base', 'shoulder', 'elbow', 'wrist1', 'wrist2', 'wrist3',],
                datasets: [{
                    label: 'Degrees of Joints',
                    data: [joints[0], joints[1], joints[2], joints[3], joints[4], joints[5]],
                }]
            },
            options: {
                indexAxis: 'y',
                scales: {
                    x: {
                        suggestedMin: -180,
                        suggestedMax: 180
                    },
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }
    else {

        jointChart.data.datasets[0].data = [joints[0], joints[1], joints[2], joints[3], joints[4], joints[5]];

        jointChart.update();
    }

};